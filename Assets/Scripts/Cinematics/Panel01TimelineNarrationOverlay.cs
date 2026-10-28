using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Renders the locked Panel 01 text only while its Timeline ActivationTrack is live.
/// Does not move the camera or advance any gameplay progression.
/// </summary>
[DisallowMultipleComponent]
public sealed class Panel01TimelineNarrationOverlay : MonoBehaviour
{
    public PlayableDirector timelineDirector;
    public double shotDuration = 10.5;
    public float fadeSeconds = 0.55f;

    private const string LockedText =
        "Once upon a time, a wizard named Van Aert lived peacefully with his daughter, Nevali...";

    private GUIStyle style;
    private GUIStyle shadowStyle;

    private void OnGUI()
    {
        if (!Application.isPlaying || timelineDirector == null ||
            timelineDirector.playableAsset == null)
            return;

        double t = timelineDirector.time;
        float a = Mathf.Clamp01((float)(t / Mathf.Max(0.01f, fadeSeconds)));
        a = Mathf.Min(a, Mathf.Clamp01((float)((shotDuration - t) / 1.10)));
        if (a < 0.001f)
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                richText = false
            };
            shadowStyle = new GUIStyle(style);
        }

        float scale = Mathf.Clamp(Screen.height / 720f, 0.65f, 2f);
        style.fontSize = Mathf.RoundToInt(23f * scale);
        shadowStyle.fontSize = style.fontSize;

        float x = Screen.width * 0.032f;
        float y = Screen.height * 0.063f;
        float width = Screen.width * 0.49f;
        float height = Mathf.Max(104f * scale, Screen.height * 0.19f);
        var rect = new Rect(x, y, width, height);

        Color previous = GUI.color;
        GUI.color = new Color(0.035f, 0.025f, 0.021f, 0.71f * a);
        GUI.DrawTexture(new Rect(rect.x - 10f * scale, rect.y - 4f * scale,
            rect.width + 20f * scale, rect.height + 8f * scale),
            Texture2D.whiteTexture);
        GUI.color = new Color(0.87f, 0.67f, 0.31f, a);
        GUI.DrawTexture(new Rect(rect.x - 11f * scale, rect.y - 4f * scale,
            Mathf.Max(2f, 3f * scale), rect.height + 8f * scale),
            Texture2D.whiteTexture);

        GUI.color = new Color(1f, 1f, 1f, a);
        style.normal.textColor = new Color(1f, 0.94f, 0.83f, 1f);
        shadowStyle.normal.textColor = new Color(0.06f, 0.035f, 0.025f, 0.85f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f,
            rect.width, rect.height), LockedText, shadowStyle);
        GUI.Label(rect, LockedText, style);
        GUI.color = previous;
    }
}
