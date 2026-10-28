using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(MovementofPlayer))]
[RequireComponent(typeof(AttackController))]
public sealed class PersistentPlayerRoot : MonoBehaviour
{
    public static PersistentPlayerRoot Instance { get; private set; }

    private static readonly HashSet<string> PlayerTransformFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "player",
        "playerTransform"
    };

    private PlayerHealth playerHealth;
    private MovementofPlayer movement;
    private AttackController attackController;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private int lastBoundSceneHandle = int.MinValue;

    public PlayerHealth Health => playerHealth;
    public MovementofPlayer Movement => movement;
    public AttackController Attack => attackController;

    private void Awake()
    {
        // Cinematic-only copies of Van Aert retain this component but have it
        // disabled intentionally. Unity still calls Awake on disabled
        // MonoBehaviours, so without this guard a dialogue actor could claim
        // the persistent player singleton and survive the story transition.
        // Its CharacterController is disabled, which then prevents movement in
        // TutorialScene and causes the real gameplay prefab to be discarded.
        if (!enabled)
            return;

        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerHealth = GetComponent<PlayerHealth>();
        movement = GetComponent<MovementofPlayer>();
        attackController = GetComponent<AttackController>();
        characterController = GetComponent<CharacterController>();
        playerRigidbody = GetComponent<Rigidbody>();

        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.handle != lastBoundSceneHandle)
            BindLoadedScene(activeScene);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindLoadedScene(scene);
    }

    public void RebindCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            BindLoadedScene(scene, force: true);
    }

    private void BindLoadedScene(Scene scene, bool force = false)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        if (!force && scene.handle == lastBoundSceneHandle)
            return;

        lastBoundSceneHandle = scene.handle;

        // Stage 2 has runtime-only water and NavMesh registration that used to
        // depend solely on RuntimeInitializeOnLoad/sceneLoaded timing. A DDOL
        // player is the stable cross-scene authority, so explicitly ensure these
        // systems whenever it binds Stage2Scene. Both calls are idempotent.
        if (scene.name == "Stage2Scene")
        {
            Stage2WaterRuntimeBootstrap.EnsureForScene(scene);
            Stage2NavMeshRuntimeBootstrap.EnsureForScene(scene);
        }

        if (!PlayerSpawnPoint.TryFind(scene, out PlayerSpawnPoint spawnPoint))
        {
            movement.enabled = false;
            attackController.enabled = false;
            PersistentHUDRoot.Instance?.SetGameplayVisible(false);
            return;
        }

        movement.enabled = true;
        attackController.enabled = true;
        PersistentHUDRoot hud = PersistentHUDRoot.Instance;
        hud?.SetGameplayVisible(true);
        hud?.HideEnemyHealth();

        // Stage 3 is authored at the original Van Aert world scale. Keep that
        // scale authoritative at runtime instead of allowing an accidentally
        // reset spawn marker to shrink the persistent player back to 1.
        Vector3 targetScale = scene.name == "Stage3Scene"
            ? Vector3.one * 10f
            : spawnPoint.PlayerScale;
        TeleportTo(spawnPoint.SpawnPose, targetScale);
        RebindSceneDependencies(scene, hud);
    }

    private void TeleportTo(Pose pose, Vector3 playerScale)
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (characterController != null)
            characterController.enabled = false;

        transform.SetPositionAndRotation(pose.position, pose.rotation);
        transform.localScale = playerScale;

        // Unity warns (and ignores the assignment) when velocity is written to a
        // kinematic Rigidbody. Scene-spawn teleportation only needs this reset for
        // dynamic bodies; the transform teleport below is authoritative either way.
        if (playerRigidbody != null && !playerRigidbody.isKinematic)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        movement.ResetTransientStateForSceneTransition();

        // Animation Rigging captures the player's scale when its playable
        // graph is built. Stage 3 changes Van from 1x to 10x here, so bind
        // V002 to the new physical scale before re-enabling collisions.
        var footRigV002 = GetComponent<VanAnimationRiggingFootPlacementV002>();
        if (footRigV002 != null && footRigV002.enabled &&
            !footRigV002.RebuildAfterStageTeleport())
        {
            footRigV002.enabled = false;
            var fallbackFootIK = GetComponent<VanProceduralFootPlacement>();
            if (fallbackFootIK != null) fallbackFootIK.enabled = true;
            Debug.LogError("[VAN FOOT IK] V002 rig scale rebind failed; V001 fallback restored.", this);
        }

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    private void RebindSceneDependencies(Scene scene, PersistentHUDRoot hud)
    {
        TileManager sceneTileManager = FindFirstInScene<TileManager>(scene);
        TileManager tileManager = TileManager.IsPersistentCombatScene(scene.name)
            ? TileManager.Instance ?? sceneTileManager
            : sceneTileManager;

        if (tileManager != null && TileManager.IsPersistentCombatScene(scene.name))
            tileManager.ConfigureForScene(scene);

        if (hud != null)
        {
            hud.CacheReferences();
            playerHealth.BindHealthSlider(hud.HealthSlider);

            if (tileManager != null)
            {
                tileManager.RebindPersistentHud(
                    hud.TilePoolPanel,
                    hud.WordBarPanel,
                    attackController,
                    clearExistingTiles: true);
            }

            // TileSpawner is scene-owned while the combat HUD is persistent.
            // Bind it to the same canonical TilePoolPanel used by TileManager so
            // Stage 3 gets the exact Stage 1/2 tile pipeline instead of a second,
            // disconnected pool.
            TileSpawner tileSpawner = FindFirstInScene<TileSpawner>(scene);
            if (tileSpawner != null)
                tileSpawner.tilePoolPanel = hud.TilePoolPanel;

            attackController.RebindSceneDependencies(hud.AttackButton, tileManager);
        }
        else
        {
            attackController.RebindSceneDependencies(attackController.attackButton, tileManager);
        }

        int rebound = RebindSerializedPlayerReferences(scene);
        hud?.RefreshPersistentPresentation();
        Debug.Log($"[PersistentPlayerRoot] Bound '{scene.name}' at {transform.position}. Rebound {rebound} player reference(s).", this);
    }

    private int RebindSerializedPlayerReferences(Scene scene)
    {
        int reboundCount = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                PersistentPlayerRoot owner = behaviour.GetComponentInParent<PersistentPlayerRoot>(true);
                if (owner != null)
                    continue;

                Type type = behaviour.GetType();
                while (type != null && type != typeof(MonoBehaviour))
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);

                    foreach (FieldInfo field in fields)
                    {
                        if (field.IsStatic || field.IsInitOnly)
                            continue;

                        bool serialized = field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField));
                        if (!serialized)
                            continue;

                        object replacement = null;
                        if (field.FieldType == typeof(PlayerHealth))
                            replacement = playerHealth;
                        else if (field.FieldType == typeof(MovementofPlayer))
                            replacement = movement;
                        else if (field.FieldType == typeof(AttackController))
                            replacement = attackController;
                        else if (field.FieldType == typeof(TileManager) && TileManager.Instance != null)
                            replacement = TileManager.Instance;
                        else if (field.FieldType == typeof(Transform) && PlayerTransformFieldNames.Contains(field.Name))
                            replacement = transform;
                        else if (field.FieldType == typeof(GameObject) && string.Equals(field.Name, "player", StringComparison.OrdinalIgnoreCase))
                            replacement = gameObject;

                        if (replacement == null || ReferenceEquals(field.GetValue(behaviour), replacement))
                            continue;

                        field.SetValue(behaviour, replacement);
                        reboundCount++;
                    }

                    type = type.BaseType;
                }
            }
        }

        return reboundCount;
    }

    private static T FindFirstInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
