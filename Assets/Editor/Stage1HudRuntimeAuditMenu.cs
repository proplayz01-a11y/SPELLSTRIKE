#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class Stage1HudRuntimeAuditMenu
{
    private const string TriggerPath = "Temp/Stage1HudAudit.trigger";

    [InitializeOnLoadMethod]
    private static void InstallWatcher()
    {
        EditorApplication.update -= CheckTrigger;
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (!System.IO.File.Exists(TriggerPath))
            return;
        try { System.IO.File.Delete(TriggerPath); } catch { return; }
        LogHudRuntimeMetrics();
    }
    [MenuItem("SPELLSTRIKE/Optimization/8. Log Stage1 HUD Runtime Metrics _F9")]
    private static void LogHudRuntimeMetrics()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[Stage1HUDAudit] Enter Play Mode first.");
            return;
        }

        var log = new StringBuilder(8192);
        log.AppendLine($"[Stage1HUDAudit] Screen={Screen.width}x{Screen.height} safe={Screen.safeArea} paused={EditorApplication.isPaused}");

        string[] names =
        {
            "HealthBar", "EnemyHealthBar", "TilePoolPanelDesign", "TilePoolPanelDesign (1)",
            "TilePoolPanel", "WordBarPanel", "UI_Buttons", "AttackButton",
            "MobileMovementJoystick", "MobileInteractionButton", "StrikesCanvas", "StrikesButton",
            "HUDCanvas", "SpellbookCanvas"
        };

        foreach (string name in names)
            AppendNamedRect(log, name);

        log.AppendLine("-- Canvas inventory --");
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            log.AppendLine(
                $"Canvas path={BuildPath(canvas.transform)} root={canvas.isRootCanvas} active={canvas.gameObject.activeInHierarchy} " +
                $"mode={canvas.renderMode} scaleFactor={canvas.scaleFactor:0.###} " +
                (scaler == null
                    ? "scaler=<none>"
                    : $"scaler={scaler.uiScaleMode}/{scaler.screenMatchMode} ref={scaler.referenceResolution} match={scaler.matchWidthOrHeight:0.###}"));
        }

        Debug.Log(log.ToString());
    }

    private static void AppendAllNamedRects(StringBuilder log, string objectName)
    {
        int count = 0;
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name != objectName || transform is not RectTransform rect)
                continue;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            log.AppendLine(
                $"{objectName}[{count++}]: path={BuildPath(rect)} rect={rect.rect} anchor=({rect.anchorMin}->{rect.anchorMax}) " +
                $"pivot={rect.pivot} pos={rect.anchoredPosition} scale={rect.localScale} active={rect.gameObject.activeInHierarchy} " +
                $"worldBL={corners[0]} worldTR={corners[2]} " +
                $"canvas={(canvas != null ? canvas.name : "<none>")} canvasScale={(canvas != null ? canvas.scaleFactor : 0f):0.###} " +
                (scaler == null ? "scaler=<none>" : $"scaler={scaler.uiScaleMode} ref={scaler.referenceResolution} match={scaler.matchWidthOrHeight:0.###}"));
        }

        if (count == 0)
            log.AppendLine($"{objectName}=<missing RectTransform>");
    }
    private static void AppendNamedRect(StringBuilder log, string objectName)
    {
        Transform found = null;
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == objectName)
            {
                found = transform;
                break;
            }
        }

        RectTransform rect = found as RectTransform;
        if (rect == null)
        {
            log.AppendLine($"{objectName}=<missing RectTransform>");
            return;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        log.AppendLine(
            $"{objectName}: path={BuildPath(rect)} rect={rect.rect} anchor=({rect.anchorMin}->{rect.anchorMax}) " +
            $"pivot={rect.pivot} pos={rect.anchoredPosition} scale={rect.localScale} active={rect.gameObject.activeInHierarchy} " +
            $"worldBL={corners[0]} worldTR={corners[2]} " +
            $"canvas={(canvas != null ? canvas.name : "<none>")} canvasScale={(canvas != null ? canvas.scaleFactor : 0f):0.###} " +
            (scaler == null ? "scaler=<none>" : $"scaler={scaler.uiScaleMode} ref={scaler.referenceResolution} match={scaler.matchWidthOrHeight:0.###}"));
    }

    private static string BuildPath(Transform transform)
    {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }
}
#endif




