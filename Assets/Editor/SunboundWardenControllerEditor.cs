#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SunboundWardenController))]
public sealed class SunboundWardenControllerEditor : Editor
{
    private const string RadiantRushClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_RadiantRush_ShortHeavy.anim";

    private SerializedProperty frameRateProperty;
    private SerializedProperty sprintStartFrameProperty;
    private SerializedProperty swordStartFrameProperty;
    private SerializedProperty swordAttackRangeProperty;
    private SerializedProperty stoppingDistanceProperty;
    private AnimationClip radiantRushClip;

    private void OnEnable()
    {
        frameRateProperty = serializedObject.FindProperty("radiantRushClipFrameRate");
        sprintStartFrameProperty = serializedObject.FindProperty("radiantRushSprintStartFrame");
        swordStartFrameProperty = serializedObject.FindProperty("radiantRushSwordStartFrame");
        swordAttackRangeProperty = serializedObject.FindProperty("radiantRushSwordAttackRange");
        stoppingDistanceProperty = serializedObject.FindProperty("radiantRushStoppingDistance");
        radiantRushClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RadiantRushClipPath);

        EditorApplication.update -= RepaintWhilePlaying;
        EditorApplication.update += RepaintWhilePlaying;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintWhilePlaying;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "radiantRushClipFrameRate",
            "radiantRushSprintStartFrame",
            "radiantRushSwordStartFrame",
            "radiantRushSwordAttackRange",
            "radiantRushStoppingDistance");

        EditorGUILayout.Space(8f);
        DrawRadiantRushTimingPanel();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRadiantRushTimingPanel()
    {
        SunboundWardenController controller = (SunboundWardenController)target;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Radiant Rush Phase Timing", EditorStyles.boldLabel);

        float frameRate = Mathf.Max(1f, frameRateProperty.floatValue);
        int sprintStartFrame = Mathf.Max(0, sprintStartFrameProperty.intValue);
        int swordStartFrame = Mathf.Max(sprintStartFrame + 1, swordStartFrameProperty.intValue);
        float swordRange = Mathf.Max(0.1f, swordAttackRangeProperty.floatValue);
        float stopDistance = Mathf.Clamp(stoppingDistanceProperty.floatValue, 0.05f, swordRange);

        EditorGUI.BeginChangeCheck();
        frameRate = Mathf.Max(1f, EditorGUILayout.FloatField("Source Clip FPS", frameRate));
        sprintStartFrame = Mathf.Max(0, EditorGUILayout.IntField("Sprint Handoff Frame", sprintStartFrame));
        swordStartFrame = Mathf.Max(
            sprintStartFrame + 1,
            EditorGUILayout.IntField("Sword Start Frame", swordStartFrame));
        swordRange = Mathf.Max(0.1f, EditorGUILayout.FloatField("Sword Attack Range", swordRange));
        stopDistance = Mathf.Clamp(
            EditorGUILayout.FloatField("Sprint Stopping Distance", stopDistance),
            0.05f,
            swordRange);

        if (EditorGUI.EndChangeCheck())
        {
            frameRateProperty.floatValue = frameRate;
            sprintStartFrameProperty.intValue = sprintStartFrame;
            swordStartFrameProperty.intValue = swordStartFrame;
            swordAttackRangeProperty.floatValue = swordRange;
            stoppingDistanceProperty.floatValue = Mathf.Min(stopDistance, swordRange);
        }

        float clipLength = GetSourceClipLength();
        float sprintStart = clipLength > 0f
            ? Mathf.Clamp01((sprintStartFrame / frameRate) / clipLength)
            : 0f;
        float swordStart = clipLength > 0f
            ? Mathf.Clamp01((swordStartFrame / frameRate) / clipLength)
            : 0f;

        DrawTimeline(controller, sprintStart, swordStart);

        if (clipLength > 0f)
        {
            EditorGUILayout.LabelField(
                "Startup -> Sprint",
                $"F{sprintStartFrame} ({sprintStart * clipLength:0.000}s)");
            EditorGUILayout.LabelField(
                "Sprint -> Sword",
                $"when Van <= {swordRange:0.00}u, sword resumes at F{swordStartFrame} ({swordStart * clipLength:0.000}s)");
            EditorGUILayout.LabelField(
                "Sprint Stop Gap",
                $"{Mathf.Min(stopDistance, swordRange):0.00}u minimum gap before sword commit");
        }

        EditorGUILayout.LabelField("Live Phase", controller.RadiantRushPhaseName);

        float normalized = controller.RadiantRushNormalizedTime;
        string liveText = normalized >= 0f
            ? $"t={normalized:0.000}"
            : "Not currently in Radiant Rush";
        EditorGUILayout.LabelField("Live State Time", liveText);

        EditorGUILayout.HelpBox(
            "Radiant Rush now uses three Animator states: Start uses the original opening, " +
            "Sprint uses the dedicated sword-and-shield run and loops naturally, and Sword resumes the original " +
            "Radiant Rush clip at the configured sword frame. No manual frame looping is used.",
            MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void DrawTimeline(
        SunboundWardenController controller,
        float sprintStart,
        float swordStart)
    {
        Rect rect = GUILayoutUtility.GetRect(10f, 34f, GUILayout.ExpandWidth(true));
        rect = EditorGUI.IndentedRect(rect);

        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f, 1f));
        DrawTimelineMarker(rect, sprintStart, new Color(0.25f, 0.95f, 0.45f, 1f), 3f);
        DrawTimelineMarker(rect, swordStart, new Color(1f, 0.45f, 0.18f, 1f), 3f);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(rect, "STARTUP          SPRINT (separate clip)          SWORD", labelStyle);
    }

    private static void DrawTimelineMarker(Rect rect, float normalized, Color color, float width)
    {
        float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(normalized));
        EditorGUI.DrawRect(new Rect(x - width * 0.5f, rect.y, width, rect.height), color);
    }

    private float GetSourceClipLength()
    {
        if (radiantRushClip == null)
            radiantRushClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RadiantRushClipPath);

        return radiantRushClip != null ? radiantRushClip.length : 0f;
    }

    private void OnSceneGUI()
    {
        SunboundWardenController controller = (SunboundWardenController)target;
        float swordRange = Mathf.Max(0f, controller.radiantRushSwordAttackRange);
        float stopDistance = Mathf.Clamp(
            controller.radiantRushStoppingDistance,
            0f,
            swordRange);
        float sunSlashRange = Mathf.Max(0f, controller.sunbindSunSlashRange);
        Vector3 center = controller.transform.position + Vector3.up * 0.15f;

        Handles.color = new Color(1f, 0.35f, 0.12f, 1f);
        Handles.DrawWireDisc(center, Vector3.up, swordRange);
        Handles.Label(
            center + Vector3.up * 0.45f,
            $"Radiant Rush Sword Range: {swordRange:0.00}u",
            EditorStyles.boldLabel);

        Handles.color = new Color(0.2f, 0.85f, 1f, 1f);
        Handles.DrawWireDisc(center, Vector3.up, stopDistance);
        Handles.Label(
            center + Vector3.up * 0.7f,
            $"Sprint Stop: {stopDistance:0.00}u",
            EditorStyles.boldLabel);

        Handles.color = new Color(0.95f, 0.82f, 0.2f, 1f);
        Handles.DrawWireDisc(center, Vector3.up, sunSlashRange);
        Handles.Label(
            center + Vector3.up * 0.95f,
            $"Sunbind -> Sun Slash Range: {sunSlashRange:0.00}u",
            EditorStyles.boldLabel);
    }

    private void RepaintWhilePlaying()
    {
        Repaint();
        SceneView.RepaintAll();
    }
}
#endif
