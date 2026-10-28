#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.VFX;

/// <summary>Explicit isolated preview of the real .vfx asset; never instantiates gameplay.</summary>
public sealed class BasicChargeStreakPreviewWindow : EditorWindow
{
    private const string GraphPath = "Assets/VFX/BasicAttackCharge/BasicChargeStreaks.vfx";
    private const string UiPath = "Assets/Editor/BasicChargeStreakPreviewWindow.uxml";
    private const string AppearancePath = "Assets/VFX/BasicAttackCharge/BasicChargeStreakAppearance.shadergraph";
    private const float Step = 1f / 30f;
    private const int CaptureFrames = 60;
    private PreviewRenderUtility preview;
    private VisualEffect effect;
    private IMGUIContainer renderSurface;
    private Slider chargeSlider;
    private Label status;
    private Label message;
    private Toggle playingToggle;
    private Toggle slowToggle;
    private Toggle brightToggle;
    private FloatField distanceField;
    private float charge = 0.5f;
    private float distance = 1.25f;
    private bool playing = true;
    private bool slow;
    private bool bright;
    private bool singleStep;
    private bool saveStill;
    private bool recording;
    private string captureDirectory;
    private int captureIndex;
    private double lastUpdate;
    private float accumulator;
    private float simulationTime;
    private int settleSteps;
    private Volume previewVolume;
    private VolumeProfile previewProfile;
    private Bloom previewBloom;
    private UniversalAdditionalCameraData previewCameraData;
    private bool bloomEnabled = true;
    private float bloomIntensity = 0.35f;
    private float emissionLow = 0.35f;
    private float emissionHigh = 6f;
    private float edgeSoftness = 1.4f;

    [MenuItem("SPELLSTRIKE/VFX/Layer 1 - Streak Graph Preview")]
    public static void Open() => GetWindow<BasicChargeStreakPreviewWindow>("Layer 1 · VFX Graph");

    private void OnEnable()
    {
        minSize = new Vector2(760, 700);
        lastUpdate = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath);
        if (tree == null)
        {
            rootVisualElement.Add(new Label("Missing preview UXML: " + UiPath));
            return;
        }
        tree.CloneTree(rootVisualElement);
        chargeSlider = rootVisualElement.Q<Slider>("charge");
        playingToggle = rootVisualElement.Q<Toggle>("playing");
        slowToggle = rootVisualElement.Q<Toggle>("slow");
        brightToggle = rootVisualElement.Q<Toggle>("bright");
        distanceField = rootVisualElement.Q<FloatField>("distance");
        status = rootVisualElement.Q<Label>("status");
        message = rootVisualElement.Q<Label>("message");
        chargeSlider.SetValueWithoutNotify(charge);
        playingToggle.SetValueWithoutNotify(playing);
        slowToggle.SetValueWithoutNotify(slow);
        brightToggle.SetValueWithoutNotify(bright);
        distanceField.SetValueWithoutNotify(distance);
        var bloomToggle = rootVisualElement.Q<Toggle>("bloom");
        var bloomField = rootVisualElement.Q<FloatField>("bloom-intensity");
        var lowField = rootVisualElement.Q<FloatField>("emission-low");
        var highField = rootVisualElement.Q<FloatField>("emission-high");
        var softnessField = rootVisualElement.Q<FloatField>("edge-softness");
        bloomToggle.SetValueWithoutNotify(bloomEnabled);
        bloomField.SetValueWithoutNotify(bloomIntensity);
        lowField.SetValueWithoutNotify(emissionLow);
        highField.SetValueWithoutNotify(emissionHigh);
        softnessField.SetValueWithoutNotify(edgeSoftness);
        bloomToggle.RegisterValueChangedCallback(e => { bloomEnabled = e.newValue; Repaint(); });
        bloomField.RegisterValueChangedCallback(e => { bloomIntensity = Mathf.Clamp(e.newValue, 0f, 2f); Repaint(); });
        lowField.RegisterValueChangedCallback(e => { emissionLow = Mathf.Clamp(e.newValue, 0f, 4f); ApplyParameters(); });
        highField.RegisterValueChangedCallback(e => { emissionHigh = Mathf.Clamp(e.newValue, 0f, 12f); ApplyParameters(); });
        softnessField.RegisterValueChangedCallback(e => { edgeSoftness = Mathf.Clamp(e.newValue, 0.5f, 4f); ApplyParameters(); });
        chargeSlider.RegisterValueChangedCallback(e => { charge = e.newValue; ApplyParameters(); });
        playingToggle.RegisterValueChangedCallback(e => playing = e.newValue);
        slowToggle.RegisterValueChangedCallback(e => slow = e.newValue);
        brightToggle.RegisterValueChangedCallback(e => bright = e.newValue);
        distanceField.RegisterValueChangedCallback(e => distance = Mathf.Clamp(e.newValue, 0.2f, 10f));
        rootVisualElement.Q<Button>("q0").clicked += () => SetCharge(0f);
        rootVisualElement.Q<Button>("q50").clicked += () => SetCharge(0.5f);
        rootVisualElement.Q<Button>("q96").clicked += () => SetCharge(0.96f);
        rootVisualElement.Q<Button>("restart").clicked += Restart;
        rootVisualElement.Q<Button>("step").clicked += () => { playing = false; playingToggle.SetValueWithoutNotify(false); singleStep = true; Repaint(); };
        rootVisualElement.Q<Button>("still").clicked += () => { saveStill = true; Repaint(); };
        rootVisualElement.Q<Button>("record").clicked += StartRecording;
        renderSurface = new IMGUIContainer(DrawGraph);
        renderSurface.AddToClassList("render-surface");
        rootVisualElement.Q<VisualElement>("preview").Add(renderSurface);
    }

    private void InitializePreview()
    {
        if (preview != null) return;
        var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        if (graph == null) throw new InvalidOperationException("VFX Graph failed to import: " + GraphPath);
        preview = new PreviewRenderUtility();
        preview.camera.fieldOfView = 40f;
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = 30f;
        preview.camera.allowHDR = true;
        preview.camera.allowMSAA = false;
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        foreach (var light in preview.lights) light.enabled = false;
        previewCameraData = preview.camera.GetUniversalAdditionalCameraData();
        previewCameraData.renderShadows = false;
        previewCameraData.requiresColorOption = CameraOverrideOption.Off;
        previewCameraData.requiresDepthOption = CameraOverrideOption.Off;
        previewCameraData.volumeLayerMask = 1 << 31;
        previewCameraData.volumeTrigger = preview.camera.transform;
        var volumeObject = new GameObject("Layer 1 — isolated preview bloom");
        volumeObject.hideFlags = HideFlags.HideAndDontSave;
        volumeObject.layer = 31;
        preview.AddSingleGO(volumeObject);
        previewVolume = volumeObject.AddComponent<Volume>();
        previewVolume.enabled = false;
        previewVolume.isGlobal = true;
        previewProfile = CreateInstance<VolumeProfile>();
        previewProfile.hideFlags = HideFlags.HideAndDontSave;
        previewVolume.sharedProfile = previewProfile;
        previewBloom = previewProfile.Add<Bloom>(true);
        previewBloom.threshold.Override(1f);
        previewBloom.scatter.Override(0.4f);
        previewBloom.clamp.Override(12f);
        previewBloom.highQualityFiltering.Override(false);
        var go = new GameObject("Layer 1 — actual VFX Graph preview");
        go.hideFlags = HideFlags.HideAndDontSave;
        effect = go.AddComponent<VisualEffect>();
        preview.AddSingleGO(go);
        effect.visualEffectAsset = graph;
        effect.resetSeedOnPlay = false;
        effect.startSeed = 137u;
        effect.pause = true;
        ApplyParameters();
        effect.Reinit();
        effect.pause = true;
    }

    private void ApplyParameters()
    {
        if (effect == null) return;
        if (effect.HasFloat("Charge01")) effect.SetFloat("Charge01", charge);
        if (effect.HasFloat("ChargeVisibility")) effect.SetFloat("ChargeVisibility", 1f);
        if (effect.HasFloat("EmissionLow")) effect.SetFloat("EmissionLow", emissionLow);
        if (effect.HasFloat("EmissionHigh")) effect.SetFloat("EmissionHigh", emissionHigh);
        if (effect.HasFloat("EdgeSoftness")) effect.SetFloat("EdgeSoftness", edgeSoftness);
        Repaint();
    }

    private void SetCharge(float value)
    {
        if (recording) return;
        charge = value;
        chargeSlider?.SetValueWithoutNotify(value);
        Restart();
        playing = false;
        playingToggle?.SetValueWithoutNotify(false);
        settleSteps = 48;
        message.text = "Settling 48 rendered steps, then pausing at exactly 1.60s for comparison.";
    }

    private void Restart()
    {
        InitializePreview();
        simulationTime = 0f;
        settleSteps = 0;
        accumulator = 0f;
        ApplyParameters();
        effect.Reinit();
        effect.pause = true;
        message.text = "Reset at fixed seed. Let trails develop, then capture. This is isolated graph evidence.";
        Repaint();
    }

    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float elapsed = Mathf.Min(0.1f, (float)(now - lastUpdate));
        lastUpdate = now;
        if (playing && !recording) accumulator += elapsed * (slow ? 0.25f : 1f);
        if (playing || recording || singleStep || saveStill || settleSteps > 0)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            renderSurface?.MarkDirtyRepaint();
            Repaint();
        }
    }

    private void StartRecording()
    {
        if (recording || settleSteps > 0) return;
        InitializePreview();
        captureDirectory = Path.Combine("tmp", "basic-charge-layer1", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-hdr-q" + charge.ToString("0.00", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(captureDirectory);
        captureIndex = 0;
        recording = true;
        message.text = "Recording 60 real graph renders at 1/30 simulation step. Do not change graph while recording.";
        Repaint();
    }

    private void DrawGraph()
    {
        Rect area = GUILayoutUtility.GetRect(100f, 100f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint || area.width < 20f || area.height < 20f) return;
        try
        {
            InitializePreview();
            if (recording || singleStep || settleSteps > 0 || (playing && accumulator >= Step))
            {
                effect.Simulate(Step, 1u);
                simulationTime += Step;
                accumulator = Mathf.Max(0f, accumulator - Step);
                singleStep = false;
                if (settleSteps > 0 && --settleSteps == 0)
                    message.text = "Ready at 1.60s, paused. Save frame or record motion; camera and seed stay fixed.";
            }
            preview.camera.backgroundColor = bright ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.025f, 0.03f, 0.045f);
            preview.camera.transform.position = new Vector3(0f, 0f, -distance);
            preview.camera.transform.rotation = Quaternion.identity;
            preview.BeginPreview(new Rect(0f, 0f, 960f, 720f), GUIStyle.none);
            previewBloom.intensity.Override(bloomIntensity);
            previewCameraData.renderPostProcessing = bloomEnabled;
            try
            {
                // Register this preview-only volume solely for this synchronous camera render.
                // Production volume profiles and renderer assets are never edited.
                previewVolume.enabled = bloomEnabled;
                preview.Render(true);
            }
            finally
            {
                previewVolume.enabled = false;
            }
            Texture texture = preview.EndPreview();
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, false);
            bool controlsLinked = effect.HasFloat("EmissionLow") && effect.HasFloat("EmissionHigh") && effect.HasFloat("EdgeSoftness");
            status.text = $"Charge01 {charge:0.00} | Simulated {simulationTime:0.00}s | Alive {effect.aliveParticleCount} (GPU readback) | Bloom {(bloomEnabled ? "ON" : "OFF")} | Appearance controls {(controlsLinked ? "linked" : "not exposed; graph defaults used")}";
            if (saveStill)
            {
                string folder = Path.Combine("tmp", "basic-charge-layer1");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-hdr-q" + charge.ToString("0.00", CultureInfo.InvariantCulture) + (bloomEnabled ? "-bloom" : "-raw") + ".png");
                SaveTexture(texture, path);
                message.text = "Saved real VFX Graph render: " + path;
                saveStill = false;
            }
            if (recording)
            {
                SaveTexture(texture, Path.Combine(captureDirectory, "frame-" + captureIndex.ToString("D3") + ".png"));
                captureIndex++;
                if (captureIndex >= CaptureFrames)
                {
                    recording = false;
                    File.WriteAllText(Path.Combine(captureDirectory, "capture.txt"),
                        $"Graph: {GraphPath}\nAppearance: {AppearancePath}\nCharge01: {charge.ToString(CultureInfo.InvariantCulture)}\nBloom: {bloomEnabled}; intensity {bloomIntensity}\nEmissionLow/High: {emissionLow}/{emissionHigh}\nEdgeSoftness: {edgeSoftness}; controls linked {controlsLinked}\nFrames: {CaptureFrames}\nSimulation step: 1/30 s\nSeed: 137\nNormal playback: 30 fps; slow-motion playback: 7.5 fps\nEditor isolated capture, not gameplay or physical A05s proof.\n");
                    message.text = "Saved 60 actual graph frames: " + captureDirectory;
                }
            }
        }
        catch (Exception ex)
        {
            playing = false; recording = false; saveStill = false;
            message.text = "PREVIEW ERROR: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private static void SaveTexture(Texture source, string path)
    {
        RenderTexture previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            RenderTexture.active = (RenderTexture)source;
            image = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (image != null) DestroyImmediate(image);
        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        preview?.Cleanup();
        preview = null;
        effect = null;
        if (previewProfile != null)
        {
            foreach (var component in previewProfile.components)
                if (component != null) DestroyImmediate(component);
            DestroyImmediate(previewProfile);
        }
        previewProfile = null;
        previewVolume = null;
        previewBloom = null;
        previewCameraData = null;
    }
}
#endif
