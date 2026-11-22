using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Standalone imported-effects gallery. One imported effect runs at a time;
/// source prefabs and gameplay scenes are never modified.
/// </summary>
public sealed class ImportedVfxShowcaseController : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string category;
        public string displayName;
        public Transform station;
        public GameObject effect;
    }

    [SerializeField] private Camera previewCamera;
    [SerializeField] private List<Entry> entries = new();
    [SerializeField] private float replayEverySeconds = 5f;
    [SerializeField] private float cameraDistance = 16f;
    [SerializeField] private bool autoReplay = true;

    private int selected = -1;
    private float timeSinceReplay;
    private float yaw;
    private bool hasSnappedCamera;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;

    public void Configure(Camera camera, List<Entry> prefabEntries)
    {
        previewCamera = camera;
        entries = prefabEntries ?? new List<Entry>();
        foreach (Entry item in entries)
            if (item?.effect != null)
                item.effect.SetActive(false);
    }

    private void Start()
    {
        if (entries.Count > 0)
            Select(0);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                Select(selected + 1);
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                Select(selected - 1);
            if (keyboard.downArrowKey.wasPressedThisFrame)
                MoveCategory(1);
            if (keyboard.upArrowKey.wasPressedThisFrame)
                MoveCategory(-1);
            if (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                Replay();
            if (keyboard.tKey.wasPressedThisFrame)
                autoReplay = !autoReplay;
            if (keyboard.aKey.wasPressedThisFrame)
                yaw -= 30f;
            if (keyboard.dKey.wasPressedThisFrame)
                yaw += 30f;
            if (keyboard.wKey.wasPressedThisFrame)
                cameraDistance = Mathf.Max(7f, cameraDistance - 3f);
            if (keyboard.sKey.wasPressedThisFrame)
                cameraDistance = Mathf.Min(30f, cameraDistance + 3f);
        }

        if (selected >= 0 && selected < entries.Count)
        {
            timeSinceReplay += Time.deltaTime;
            if (autoReplay && timeSinceReplay >= replayEverySeconds)
                Replay();
            UpdateCamera();
        }
    }

    private void Select(int index)
    {
        if (entries.Count == 0)
            return;

        int next = (index % entries.Count + entries.Count) % entries.Count;
        if (selected >= 0 && selected < entries.Count && entries[selected].effect != null)
            entries[selected].effect.SetActive(false);

        selected = next;
        yaw = 0f;
        timeSinceReplay = 0f;
        Replay();
    }

    private void MoveCategory(int sign)
    {
        if (selected < 0 || entries.Count == 0)
            return;

        string current = entries[selected].category;
        for (int step = 1; step < entries.Count; step++)
        {
            int next = (selected + sign * step + entries.Count * 2) % entries.Count;
            if (entries[next].category != current)
            {
                Select(next);
                return;
            }
        }
    }

    private void Replay()
    {
        if (selected < 0 || selected >= entries.Count)
            return;

        GameObject fx = entries[selected].effect;
        timeSinceReplay = 0f;
        if (fx == null)
            return;

        fx.SetActive(false);
        fx.SetActive(true);
        foreach (ParticleSystem particles in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particles == null || !particles.gameObject.activeInHierarchy)
                continue;
            particles.Clear(true);
            particles.Play(true);
        }
    }

    private void UpdateCamera()
    {
        if (previewCamera == null || entries[selected].station == null)
            return;

        Vector3 anchor = entries[selected].station.position + Vector3.up * 1.8f;
        Vector3 offset = Quaternion.Euler(0f, yaw, 0f)
            * new Vector3(0f, cameraDistance * 0.36f, -cameraDistance);
        Vector3 destination = anchor + offset;
        float t = 1f - Mathf.Exp(-5.5f * Time.unscaledDeltaTime);
        previewCamera.transform.position = hasSnappedCamera
            ? Vector3.Lerp(previewCamera.transform.position, destination, t)
            : destination;
        Quaternion rotation = Quaternion.LookRotation(anchor - previewCamera.transform.position, Vector3.up);
        previewCamera.transform.rotation = hasSnappedCamera
            ? Quaternion.Slerp(previewCamera.transform.rotation, rotation, t)
            : rotation;
        hasSnappedCamera = true;
    }

    private void OnGUI()
    {
        if (selected < 0 || selected >= entries.Count)
            return;

        headerStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.89f, 0.96f, 1f) }
        };
        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        float w = Mathf.Min(680f, Screen.width - 20f);
        GUI.Box(new Rect(10f, 10f, w, 178f), "");
        Entry entry = entries[selected];
        GUI.Label(new Rect(25f, 19f, w - 30f, 32f),
            "SPELLSTRIKE  /  IMPORTED VFX LAB", headerStyle);
        GUI.Label(new Rect(25f, 55f, w - 30f, 22f),
            $"{selected + 1}/{entries.Count}  •  {entry.category}  •  {entry.displayName}", bodyStyle);
        GUI.Label(new Rect(25f, 80f, w - 30f, 22f),
            "LEFT/RIGHT: effects     UP/DOWN: category     R/SPACE: replay", bodyStyle);
        GUI.Label(new Rect(25f, 101f, w - 30f, 22f),
            "A/D: rotate     W/S: zoom     T: auto replay " + (autoReplay ? "ON" : "OFF"), bodyStyle);
        float buttonWidth = Mathf.Min(130f, (w - 70f) / 4f);
        if (GUI.Button(new Rect(25f, 138f, buttonWidth, 30f), "Previous"))
            Select(selected - 1);
        if (GUI.Button(new Rect(32f + buttonWidth, 138f, buttonWidth, 30f), "Next"))
            Select(selected + 1);
        if (GUI.Button(new Rect(39f + buttonWidth * 2f, 138f, buttonWidth, 30f), "Replay"))
            Replay();
        if (GUI.Button(new Rect(46f + buttonWidth * 3f, 138f, buttonWidth, 30f), "Next group"))
            MoveCategory(1);
    }
}

