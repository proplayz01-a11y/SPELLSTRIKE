using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spellstrike.ComicMode
{
    // Shared UI construction for runtime-only comic and presentation selection.
    // The UI is self-contained and can be launched from the master cinematic scene.
    public static class ComicModeUi
    {
        public static Canvas MakeCanvas(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem(canvas.transform);
            return canvas;
        }

        public static void EnsureEventSystem(Transform fallbackCanvasRoot)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            // Own only the event system created for the comic. Existing gameplay
            // EventSystems are reused. The created object dies with the canvas.
            GameObject eventSystem = new GameObject(
                "ComicMode_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(fallbackCanvasRoot, false);
        }

        public static RectTransform Rect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Fill(string name, Transform parent,
            Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Label(string name, Transform parent,
            Vector2 min, Vector2 max, string text, int size,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = Rect(name, parent, min, max);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(16f, size * 0.65f);
            label.fontSizeMax = size;
            return label;
        }

        public static Button Button(string name, Transform parent,
            Vector2 min, Vector2 max, string title, int size = 27)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.14f, 0.20f, 0.33f, 0.98f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.33f, 0.48f);
            colors.pressedColor = new Color(0.08f, 0.14f, 0.24f);
            button.colors = colors;
            Label(name + "_Label", rect, Vector2.zero, Vector2.one, title, size,
                TextAlignmentOptions.Center, new Color(0.98f, 0.93f, 0.79f));
            return button;
        }

        // Full-height, completely invisible click/tap target. Keep these behind
        // visible controls in the hierarchy so Back/Skip/confirmation stay usable.
        public static Button InvisiblePanelZone(string name, Transform parent,
            Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            button.transition = Selectable.Transition.None;
            return button;
        }

        public static RectTransform SafeArea(Canvas canvas)
        {
            return Rect("SafeArea", canvas.transform, Vector2.zero, Vector2.one);
        }

        public static void UpdateSafeArea(RectTransform rect)
        {
            Rect area = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
