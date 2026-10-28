using UnityEngine;

/// <summary>
/// Defense-prototype playback for the first family-memory shot.
/// Static character poses are intentional; only the shot camera makes a
/// minimal cinematic push. Does not advance or mutate the gameplay sequence.
/// Voiceover is optional because the final VO recording has not been supplied.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class Panel01CinematicPlayback : MonoBehaviour
{
    [Header("Panel 01 - Readable 10-Second Beat")]
    [Min(1f)] public float shotDuration = 10.5f;
    [Range(0f, 0.6f)] public float pushDistance = 0.27f;
    [Range(0f, 1.5f)] public float fadeInSeconds = 0.55f;
    public bool showNarrationText = true;
    public bool holdFinalFrame = true;

    [Header("Optional Recorded Narration")]
    public AudioClip narrationVoiceOver;

    private const string LockedNarration =
        "Once upon a time, a wizard named Van Aert lived peacefully with his daughter, Nevali...";

    private float elapsed;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    private AudioSource narrationSource;
    private GUIStyle narrationStyle;
    private GUIStyle shadowStyle;

    private void OnEnable()
    {
        elapsed = 0f;
        initialCameraPosition = transform.position;
        initialCameraRotation = transform.rotation;
        narrationStyle = null;
        shadowStyle = null;

        narrationSource = GetComponent<AudioSource>();
        if (narrationVoiceOver != null)
        {
            if (narrationSource == null)
                narrationSource = gameObject.AddComponent<AudioSource>();
            narrationSource.playOnAwake = false;
            narrationSource.spatialBlend = 0f;
            narrationSource.clip = narrationVoiceOver;
            narrationSource.Play();
        }
    }

    private void Update()
    {
        if (elapsed >= shotDuration && holdFinalFrame)
            return;

        elapsed += Time.deltaTime;
        float fraction = Mathf.Clamp01(elapsed / Mathf.Max(1f, shotDuration));
        float smooth = fraction * fraction * (3f - 2f * fraction);
        transform.position = initialCameraPosition +
            initialCameraRotation * Vector3.forward * (pushDistance * smooth);
        transform.rotation = initialCameraRotation;
    }

    private void OnGUI()
    {
        if (!showNarrationText || !Application.isPlaying)
            return;

        // The original storyboard artwork stays text-free. This is a
        // runtime-only narration overlay, matching the locked Canva copy.
        float fadeIn = fadeInSeconds <= 0f
            ? 1f : Mathf.Clamp01(elapsed / fadeInSeconds);
        float fadeOut = Mathf.Clamp01((shotDuration - elapsed) / 1.1f);
        float alpha = Mathf.Min(fadeIn, fadeOut);
        if (alpha < 0.001f)
            return;

        if (narrationStyle == null)
        {
            narrationStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = false,
                fontStyle = FontStyle.Normal
            };
            shadowStyle = new GUIStyle(narrationStyle);
        }

        float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2.0f);
        narrationStyle.fontSize = Mathf.RoundToInt(23f * scale);
        shadowStyle.fontSize = narrationStyle.fontSize;
        float margin = Screen.width * 0.032f;
        float top = Screen.height * 0.063f;
        float width = Screen.width * 0.49f;
        float height = Mathf.Max(104f * scale, Screen.height * 0.19f);
        var rect = new Rect(margin, top, width, height);

        var prevColor = GUI.color;
        GUI.color = new Color(0.035f, 0.025f, 0.021f, 0.71f * alpha);
        GUI.DrawTexture(new Rect(rect.x - 10f * scale, rect.y - 4f * scale,
            rect.width + 20f * scale, rect.height + 8f * scale),
            Texture2D.whiteTexture);
        GUI.color = new Color(0.87f, 0.67f, 0.31f, alpha);
        GUI.DrawTexture(new Rect(rect.x - 11f * scale, rect.y - 4f * scale,
            Mathf.Max(2f, 3f * scale), rect.height + 8f * scale),
            Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        shadowStyle.normal.textColor = new Color(0.06f, 0.035f, 0.025f, 0.85f);
        narrationStyle.normal.textColor = new Color(1f, 0.94f, 0.83f, 1f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height),
            LockedNarration, shadowStyle);
        GUI.Label(rect, LockedNarration, narrationStyle);
        GUI.color = prevColor;
    }

    private void OnDisable()
    {
        if (narrationSource != null && narrationSource.isPlaying)
            narrationSource.Stop();
        transform.SetPositionAndRotation(initialCameraPosition, initialCameraRotation);
    }
}
