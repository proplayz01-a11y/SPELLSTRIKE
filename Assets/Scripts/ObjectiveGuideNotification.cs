using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-owned, non-interactive objective toast using the Tutorial guide art.</summary>
public sealed class ObjectiveGuideNotification : MonoBehaviour
{
    private struct Notice
    {
        public int key;
        public string message;
    }

    private readonly List<Notice> pending = new();
    private RectTransform root, panel;
    private CanvasGroup group;
    private TMP_Text messageText;
    private bool displaying;
    private float elapsed, visibility;
    private const float ReadSeconds = 4f;

    public void Initialize(RectTransform safeRoot)
    {
        root = safeRoot;
        var prefab = Resources.Load<GameObject>("Tutorial/ObjectiveGuideNotification");
        if (prefab == null) return;
        var instance = Instantiate(prefab, root, false);
        panel = instance.GetComponent<RectTransform>();
        group = instance.GetComponent<CanvasGroup>();
        messageText = instance.transform.Find("DialogueText").GetComponent<TMP_Text>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        foreach (var graphic in instance.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        panel.anchorMin = panel.anchorMax = new Vector2(1f, .68f);
        panel.pivot = new Vector2(1f, .5f);
        PositionPanel();
    }

    public void Enqueue(int key, string message)
    {
        if (panel == null || string.IsNullOrWhiteSpace(message)) return;
        // Rapid progress for the same goal replaces its queued message.
        for (int i = 0; i < pending.Count; i++)
        {
            if (pending[i].key != key) continue;
            pending[i] = new Notice { key = key, message = message };
            return;
        }
        pending.Add(new Notice { key = key, message = message });
    }

    private bool CanPresent()
    {
        var hud = PersistentHUDRoot.Instance;
        return Time.timeScale > 0f
            && (hud == null || hud.GameplayHealthVisible)
            && !SpiritRemnantCinematic.IsCinematicActive
            && !Stage1FirstFragmentReveal.IsCinematicActive
            && !Stage1PostJesterEchoCall.IsCinematicActive
            && !Stage1HollowKnightConfrontation.IsCinematicActive;
    }

    private void LateUpdate()
    {
        if (panel == null || group == null) return;
        if (!CanPresent())
        {
            // Suspend the reading timer and keep notifications out of cinematics.
            group.alpha = 0f;
            PositionPanel();
            return;
        }
        if (!displaying && pending.Count > 0)
        {
            messageText.text = pending[0].message;
            pending.RemoveAt(0);
            displaying = true;
            elapsed = 0f;
        }
        bool show = displaying && elapsed < ReadSeconds;
        visibility = Mathf.MoveTowards(visibility, show ? 1f : 0f,
            Time.unscaledDeltaTime / (show ? .4f : .45f));
        if (displaying) elapsed += Time.unscaledDeltaTime;
        group.alpha = SmoothVisibility(visibility);
        PositionPanel();
        if (displaying && !show && visibility <= 0f)
            displaying = false;
    }

    public static float SmoothVisibility(float value) => value * value * (3f - 2f * value);

    private void PositionPanel()
    {
        if (root == null || panel == null) return;
        // Preserve the exact tutorial artwork ratio and its authored text layout.
        float width = Mathf.Min(650f, root.rect.width * .32f);
        float scale = Mathf.Max(.01f, width / 650f);
        panel.localScale = Vector3.one * scale;
        panel.anchoredPosition = new Vector2(-24f + (width + 80f) *
            (1f - SmoothVisibility(visibility)), 0f);
    }
}
