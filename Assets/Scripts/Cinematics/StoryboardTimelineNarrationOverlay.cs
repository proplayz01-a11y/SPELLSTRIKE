using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Reusable, shot-local narration for storyboard Timeline rough cuts.
/// Display is controlled by a Timeline Activation Track on this object.
/// Never modifies story progression, cameras, gameplay or saved state.
/// </summary>
[DisallowMultipleComponent]
public sealed class StoryboardTimelineNarrationOverlay : MonoBehaviour
{
    public PlayableDirector director;

    [TextArea(2, 5)]
    public string narrationText =
        "...until Broken Language spread across the kingdoms, twisting Words into weapons of ruin.";

    public double shotDuration = 8.5;
    public float fadeInSeconds = 0.6f;
    public float fadeOutSeconds = 1f;

    private GUIStyle narrationStyle;

    private void OnGUI()
    {
        if (!Application.isPlaying || director == null ||
            director.playableAsset == null || string.IsNullOrEmpty(narrationText))
            return;

        double current = director.time;
        float opacity = Mathf.Min(
            Mathf.Clamp01((float)(current / Mathf.Max(0.01f, fadeInSeconds))),
            Mathf.Clamp01((float)((shotDuration - current) /
                                   Mathf.Max(0.01f, fadeOutSeconds))));

        if (opacity <= 0.002f)
            return;

        if (narrationStyle == null)
        {
            narrationStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                richText = false
            };
        }

        float scale = Mathf.Clamp(Screen.height / 720f, 0.75f, 2f);
        narrationStyle.fontSize = Mathf.RoundToInt(22f * scale);
        narrationStyle.normal.textColor = new Color(0.98f, 0.96f, 0.96f, opacity);

        var rect = new Rect(Screen.width * 0.035f, Screen.height * 0.065f,
            Screen.width * 0.59f, Screen.height * 0.19f);
        Color previous = GUI.color;

        GUI.color = new Color(0.025f, 0.021f, 0.057f, 0.78f * opacity);
        GUI.DrawTexture(new Rect(rect.x - 12f * scale, rect.y - 4f * scale,
            rect.width + 18f * scale, rect.height + 8f * scale),
            Texture2D.whiteTexture);

        GUI.color = new Color(0.66f, 0.40f, 0.91f, opacity);
        GUI.DrawTexture(new Rect(rect.x - 12f * scale, rect.y - 4f * scale,
            3f * scale, rect.height + 8f * scale), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(rect, narrationText, narrationStyle);
        GUI.color = previous;
    }
}
