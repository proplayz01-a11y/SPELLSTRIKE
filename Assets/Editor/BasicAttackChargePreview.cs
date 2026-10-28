#if UNITY_EDITOR
using System;
using System.IO;
using Spellstrike.Vfx;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit, visible preview. Never opens, saves, or edits a production scene.</summary>
public sealed class BasicAttackChargePreview : EditorWindow
{
    private const string ChargePath = "Assets/VFX/BasicAttackCharge/BasicAttackCharge.prefab";
    private const string PlayerPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string ClipPath = "Assets/VAN AERT/ANIMS/BasicAttack.anim";
    private PreviewRenderUtility preview;
    private GameObject player;
    private BasicAttackChargeVfx charge;
    private Transform spawn;
    private AnimationClip clip;
    private float progress = 0.5f;
    private float release;
    private float fadeSeconds;
    private bool playing;
    private bool showPlayer;
    private bool mobile;
    private double lastTime;
    private string verification = "Use Run checks to validate this imported asset.";

    [MenuItem("SPELLSTRIKE/VFX/Basic Attack Charge Preview")]
    public static void Open() => GetWindow<BasicAttackChargePreview>("Basic Charge Preview");

    private void OnEnable()
    {
        minSize = new Vector2(700, 510);
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private void Initialize()
    {
        if (preview != null) return;
        preview = new PreviewRenderUtility();
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = new Color(0.028f, 0.042f, 0.065f);
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = 30f;
        preview.camera.fieldOfView = 35f;
        preview.lights[0].intensity = 1.2f;
        preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
        preview.lights[1].intensity = 0.6f;
        player = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));
        player.hideFlags = HideFlags.HideAndDontSave;
        // Preview clone only. SampleAnimation does not run gameplay or release events.
        foreach (MonoBehaviour behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Collider collider in player.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Rigidbody body in player.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
        player.GetComponent<Animator>().fireEvents = false;
        preview.AddSingleGO(player);
        foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
            if (child.name == "MagicSpawnPoint") { spawn = child; break; }
        charge = Instantiate(AssetDatabase.LoadAssetAtPath<BasicAttackChargeVfx>(ChargePath), spawn, false);
        charge.gameObject.hideFlags = HideFlags.HideAndDontSave;
        clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        release = BasicAttackChargeBinding.FindReleaseTime(clip);
        fadeSeconds = player.GetComponent<BasicAttackChargeBinding>().PostReleaseFadeSeconds;
    }

    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        if (playing) progress = Mathf.Repeat(progress + (float)(now - lastTime) / Mathf.Max(release, 0.01f),
            Mathf.Max(1.5f, 1f + fadeSeconds / Mathf.Max(release, 0.01f) + 0.25f));
        lastTime = now;
        if (playing) Repaint();
    }

    private void OnGUI()
    {
        Initialize();
        EditorGUILayout.LabelField("BASIC ATTACK · 3–5 LETTERS · CHARGE ONLY", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Current charge shape → existing release → intensity fades to zero");
        EditorGUILayout.LabelField("Existing release: " + release.ToString("0.000") + " s · Sampled canonical animation · No scene writes");
        EditorGUILayout.LabelField("Post-release fade: " + fadeSeconds.ToString("0.000") + " clip seconds · Tune on the player binding, then reopen this preview");
        using (new EditorGUILayout.HorizontalScope())
        {
            playing = GUILayout.Toggle(playing, "Loop preview", "Button");
            showPlayer = GUILayout.Toggle(showPlayer, "Show Van Aert", "Button");
            mobile = GUILayout.Toggle(mobile, "Mobile budget", "Button");
            if (GUILayout.Button("Run checks")) RunChecks();
            if (GUILayout.Button("Save evidence")) SaveEvidence();
        }
        progress = EditorGUILayout.Slider("Timeline (1 = release)", progress, 0f,
            1f + fadeSeconds / Mathf.Max(release, 0.01f) + 0.1f);
        EditorGUILayout.LabelField(verification, EditorStyles.wordWrappedMiniLabel);
        Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type == EventType.Repaint)
        {
            Prepare(progress);
            preview.BeginPreview(rect, GUIStyle.none);
            preview.Render(true);
            GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
        }
    }

    private void Prepare(float q)
    {
        clip.SampleAnimation(player, Mathf.Min(q * release, clip.length));
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
            if (!renderer.transform.IsChildOf(charge.transform)) renderer.enabled = showPlayer;
        Vector3 center = showPlayer ? Vector3.Lerp(player.transform.position + Vector3.up, spawn.position, 0.38f) : spawn.position;
        Vector3 direction = new Vector3(0.8f, 0.3f, -1f).normalized;
        preview.camera.transform.position = center + direction * (showPlayer ? 5.4f : 1.65f);
        preview.camera.transform.LookAt(center);
        charge.QualityLevel = mobile ? BasicAttackChargeVfx.Quality.Mobile : BasicAttackChargeVfx.Quality.Desktop;
        float seconds = q * release;
        BasicAttackChargeTiming.Sample sample = BasicAttackChargeTiming.Evaluate(seconds, release, fadeSeconds);
        if (!sample.IsVisible) charge.Hide();
        else charge.RenderCharge(sample.Charge01, Mathf.Min(seconds, release), preview.camera, sample.Visibility);
    }

    private void RunChecks()
    {
        bool previousMobile = mobile;
        bool previousEnabled = charge.enabled;
        try
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/BasicAttackCharge/BasicChargeEnergy.mat");
            Require(material != null && material.shader != null, "Missing charge material/shader");
            Require(!ShaderUtil.ShaderHasError(material.shader), "Shader compiler reports errors");
            Require(Mathf.Abs(release - 25f / 30f) < 0.001f, "Basic release marker changed from frame 25");
            Require(player.GetComponent<BasicAttackChargeBinding>() != null, "Canonical binding missing");
            int desktopVertices = 0, mobileVertices = 0;
            foreach (bool mobileBudget in new[] { false, true })
            {
                mobile = mobileBudget;
                foreach (float q in new[] { 0f, 0.2f, 0.5f, 0.8f, 0.96f })
                {
                    Prepare(q);
                    Require(charge.IsVisible, "Charge hidden before release");
                    Require(Vector3.Distance(charge.transform.position, spawn.position) < 0.00001f, "Charge detached from staff");
                    Require(charge.GetComponentsInChildren<MeshRenderer>().Length == 1, "Unexpected renderer count");
                    Require(charge.GetComponentsInChildren<Light>().Length == 0, "Unexpected VFX light");
                    Mesh mesh = charge.GetComponentInChildren<MeshFilter>().sharedMesh;
                    foreach (Vector3 v in mesh.vertices)
                        Require(float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z), "Non-finite mesh vertex");
                    Require(mesh.vertexCount < 1600, "Vertex budget exceeded");
                    if (mobile) mobileVertices = mesh.vertexCount; else desktopVertices = mesh.vertexCount;
                }
                if (fadeSeconds > 0f)
                {
                    Prepare(1f);
                    Require(charge.IsVisible && Mathf.Approximately(charge.Visibility01, 1f), "Fade does not begin at full release intensity");
                    Color[] releaseColors = charge.GetComponentInChildren<MeshFilter>().sharedMesh.colors;
                    Prepare(1f + 0.5f * fadeSeconds / release);
                    Require(charge.IsVisible && Mathf.Approximately(charge.Charge01, 1f), "Charge disappeared or restarted during fade");
                    Require(Mathf.Abs(charge.Visibility01 - 0.5f) < 0.001f, "Halfway fade intensity mismatch");
                    Color[] fadeColors = charge.GetComponentInChildren<MeshFilter>().sharedMesh.colors;
                    Require(fadeColors.Length == releaseColors.Length, "Fade changed the geometry budget");
                    for (int i = 0; i < fadeColors.Length; i++)
                    {
                        Vector3 actual = new Vector3(fadeColors[i].r, fadeColors[i].g, fadeColors[i].b);
                        Vector3 expected = new Vector3(releaseColors[i].r, releaseColors[i].g, releaseColors[i].b) * 0.5f;
                        Require(Vector3.Distance(actual, expected) < 0.005f, "A charge layer did not dim with the shared envelope");
                    }
                }
            }
            Prepare(1f + (fadeSeconds + 0.001f) / release);
            Require(!charge.IsVisible, "Charge remains visible after fade completion");
            Prepare(0.5f); charge.enabled = false;
            // This is Edit Mode: no ExecuteAlways/runtime OnDisable callback is assumed.
            // Exercise the explicit preview render guard instead. Runtime disable needs Play Mode.
            charge.RenderCharge(0.5f, 0.5f * release, preview.camera);
            Require(!charge.IsVisible, "Disabled component accepted a preview render request"); charge.enabled = true;
            verification = $"PASS · imported shader, release marker, staff attachment, finite geometry, all-layer fade, disabled render guard · Desktop {desktopVertices} / Mobile {mobileVertices} vertices. Runtime lifecycle and A05s still require testing.";
            Directory.CreateDirectory("tmp/basic-charge-reference");
            File.WriteAllText("tmp/basic-charge-reference/unity-validation.txt", DateTime.Now.ToString("O") + "\n" + verification);
            Debug.Log("[BasicCharge] " + verification);
        }
        catch (Exception ex)
        {
            verification = "FAIL · " + ex.Message;
            Debug.LogException(ex);
        }
        finally
        {
            mobile = previousMobile;
            charge.enabled = previousEnabled;
            Prepare(progress);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private void SaveEvidence()
    {
        Directory.CreateDirectory("tmp/basic-charge-reference");
        bool oldMobile = mobile, oldPlayer = showPlayer;
        foreach (bool mobileBudget in new[] { false, true })
        {
            mobile = mobileBudget;
            showPlayer = false;
            foreach (float q in new[] { 0.05f, 0.35f, 0.65f, 0.85f, 0.96f, 1f })
                SaveFrame(q, (mobile ? "mobile" : "pc") + "-" + q.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            SaveFrame(1f + 0.5f * fadeSeconds / release, (mobile ? "mobile" : "pc") + "-fade-half");
            SaveFrame(1f + (fadeSeconds + 0.001f) / release, (mobile ? "mobile" : "pc") + "-fade-complete");
        }
        mobile = false; showPlayer = true;
        SaveFrame(0.65f, "pc-staff-attachment");
        mobile = oldMobile; showPlayer = oldPlayer;
        verification = "Saved PC/mobile-budget rendered frames to tmp/basic-charge-reference (Editor preview, not physical-device evidence).";
    }

    private void SaveFrame(float q, string name)
    {
        Prepare(q);
        preview.BeginPreview(new Rect(0, 0, 960, 720), GUIStyle.none);
        preview.Render(true);
        Texture result = preview.EndPreview();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = (RenderTexture)result;
        Texture2D image = new Texture2D(960, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); image.Apply();
        File.WriteAllBytes("tmp/basic-charge-reference/" + name + ".png", image.EncodeToPNG());
        RenderTexture.active = previous;
        DestroyImmediate(image);
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        preview?.Cleanup();
        preview = null;
    }
}
#endif
