using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Isolated A04-only defense narration. The caption does not modify gameplay UI
/// and reads its clock from the scene's PlayableDirector.
/// </summary>
public sealed class Panel04A04NarrationOverlay : MonoBehaviour
{
    public PlayableDirector director;
    public double duration = 10.0;
    [TextArea(2, 4)]
    public string narration =
        "When the war finally ended, Van returned home - only to find his wife gone " +
        "and his daughter, Nevali, nowhere to be found.";

    private GUIStyle captionStyle;
    private Texture2D background;

    private void OnGUI()
    {
        if (director == null || director.playableAsset == null)
            return;

        double time = director.time;
        if (time < 0.30 || time > duration - 0.32)
            return;

        if (captionStyle == null)
        {
            captionStyle = new GUIStyle(GUI.skin.label);
            captionStyle.alignment = TextAnchor.MiddleLeft;
            captionStyle.wordWrap = true;
            captionStyle.normal.textColor = new Color(0.99f, 0.95f, 0.84f);
            captionStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 49.0f), 16, 29);
            captionStyle.padding = new RectOffset(14, 14, 8, 8);
        }

        // Small caption in upper-right negative space; no giant HUD overlay.
        float width = Mathf.Min(Screen.width * 0.43f, 680f);
        float height = Mathf.Clamp(Screen.height * 0.12f, 64f, 118f);
        Rect area = new Rect(Screen.width - width - 26f, 34f, width, height);
        Color previous = GUI.color;
        float alpha = Mathf.Clamp01((float)((time - 0.30) / 0.75)) *
                      Mathf.Clamp01((float)((duration - 0.32 - time) / 0.75));
        GUI.color = new Color(0.025f, 0.028f, 0.040f, 0.69f * alpha);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(area, narration, captionStyle);
        GUI.color = previous;
    }
}
