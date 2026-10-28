using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerSpawnPoint : MonoBehaviour
{
    public const string DefaultSpawnId = "Default";

    [SerializeField] private string spawnId = DefaultSpawnId;
    [SerializeField] private Vector3 playerScale = Vector3.one;

#if UNITY_EDITOR
    [SerializeField, Min(0.1f)] private float gizmoRadius = 0.75f;
#endif

    public string SpawnId => string.IsNullOrWhiteSpace(spawnId) ? DefaultSpawnId : spawnId;
    public Pose SpawnPose => new Pose(transform.position, transform.rotation);
    public Vector3 PlayerScale => playerScale;

    public void Place(Transform player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        player.SetPositionAndRotation(transform.position, transform.rotation);
        player.localScale = playerScale;
    }

    public void SetPlayerScale(Vector3 scale)
    {
        playerScale = scale;
    }

    public static bool TryFind(Scene scene, out PlayerSpawnPoint spawnPoint, string requestedSpawnId = DefaultSpawnId)
    {
        spawnPoint = null;

        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        string id = string.IsNullOrWhiteSpace(requestedSpawnId) ? DefaultSpawnId : requestedSpawnId;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlayerSpawnPoint[] candidates = root.GetComponentsInChildren<PlayerSpawnPoint>(true);
            foreach (PlayerSpawnPoint candidate in candidates)
            {
                if (candidate != null && string.Equals(candidate.SpawnId, id, StringComparison.OrdinalIgnoreCase))
                {
                    spawnPoint = candidate;
                    return true;
                }
            }
        }

        // Designer-friendly fallback: a marker named PlayerSpawnPoint is enough.
        // This is especially useful in newly-authored stages before the component
        // has been serialized into the scene. The runtime component uses the
        // marker's exact position/rotation. The Tutorial uses the canonical
        // full-size Van Aert (10x); other scenes keep their existing 1x
        // fallback unless they explicitly author PlayerSpawnPoint scale.
        if (string.Equals(id, DefaultSpawnId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate == null || !string.Equals(candidate.name, "PlayerSpawnPoint", StringComparison.OrdinalIgnoreCase))
                        continue;

                    spawnPoint = candidate.GetComponent<PlayerSpawnPoint>();
                    if (spawnPoint == null)
                        spawnPoint = candidate.gameObject.AddComponent<PlayerSpawnPoint>();

                    spawnPoint.playerScale = scene.name == "TutorialScene"
                        ? Vector3.one * 10f
                        : Vector3.one;
                    Debug.Log($"[PlayerSpawnPoint] Using named marker fallback in '{scene.name}' at {candidate.position}.", candidate);
                    return true;
                }
            }
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.9f, 1f, 0.95f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * (gizmoRadius * 2f));
    }
#endif
}
