#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only runtime diagnostics for the Tutorial responsive HUD.
/// Logs hierarchy, Canvas/CanvasScaler state, world corners, and tile counts without
/// mutating scene/save data. F6 is deliberately separate from the F7/F8 layout previews.
/// </summary>
public static class TutorialHudRuntimeAuditMenu
{
    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Apply Canonical Mobile HUD _F5")]
    private static void ApplyCanonicalMobileHud()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[TutorialHUDAudit] Enter Tutorial Play Mode before applying the canonical HUD.");
            return;
        }

        // CanvasScaler does not refresh its scale factor while the Editor is paused.
        // The layout preview shortcuts are often used from a paused frame, so resume
        // first and let the normal Editor update loop settle the root canvases.
        EditorApplication.isPaused = false;

        TutorialResponsiveLayout[] layouts = Object.FindObjectsByType<TutorialResponsiveLayout>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (layouts.Length == 0)
        {
            Debug.LogError("[TutorialHUDAudit] TutorialResponsiveLayout was not found, including inactive objects.");
            return;
        }

        layouts[0].ForceRefresh();
        Debug.Log("[TutorialHUDAudit] Canonical mobile HUD applied from TutorialResponsiveLayout.");
    }

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Apply Canonical Mobile HUD _F5", true)]
    private static bool ValidateApplyCanonicalMobileHud() => Application.isPlaying;

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Log HUD Runtime Metrics _F6")]
    private static void LogHudRuntimeMetrics()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[TutorialHUDAudit] Enter Tutorial Play Mode before logging HUD metrics.");
            return;
        }

        StringBuilder log = new StringBuilder(4096);
        log.AppendLine(
            $"[TutorialHUDAudit] Screen={Screen.width}x{Screen.height} safe={Screen.safeArea} " +
            $"mobile={TutorialResponsiveLayout.IsMobilePresentation} paused={EditorApplication.isPaused}");

        TutorialHUDController hud = Object.FindFirstObjectByType<TutorialHUDController>();
        if (hud != null)
        {
            AppendRect(log, "TutorialHUD", hud.transform as RectTransform);
            AppendNamedRect(log, "SafeArea", hud.transform, "SafeArea");
            AppendNamedRect(log, "ObjectivePanel", hud.transform, "ObjectivePanel");
            AppendNamedRect(log, "GuideDialogue", hud.transform, "GuideDialogue");
            AppendNamedRect(log, "MobilePrompt", hud.transform, "MobilePrompt");
        }
        else
        {
            log.AppendLine("TutorialHUD=<missing>");
        }

        GameObject gameplay = GameObject.Find("GameplayCanvas");
        if (gameplay != null)
        {
            AppendNamedRect(log, "HealthBar", gameplay.transform, "HealthBar");
            AppendNamedRect(log, "EnemyHealthBar", gameplay.transform, "EnemyHealthBar");
            AppendNamedRect(log, "TilePoolPanelDesign", gameplay.transform, "TilePoolPanelDesign");
            AppendNamedRect(log, "TilePoolPanelDesignOverlay", gameplay.transform, "TilePoolPanelDesign (1)");
            AppendNamedRect(log, "TilePoolPanel", gameplay.transform, "TilePoolPanel");
            AppendNamedRect(log, "WordBarPanel", gameplay.transform, "WordBarPanel");
            AppendNamedRect(log, "UI_Buttons", gameplay.transform, "UI_Buttons");
            AppendNamedRect(log, "AttackButton", gameplay.transform, "AttackButton");

            Transform pool = FindNamedTransform(gameplay.transform, "TilePoolPanel");
            Transform word = FindNamedTransform(gameplay.transform, "WordBarPanel");
            log.AppendLine($"TileCounts pool={(pool != null ? pool.childCount : -1)} word={(word != null ? word.childCount : -1)}");

            AppendGrid(log, "TilePoolGrid", pool != null ? pool.GetComponent<GridLayoutGroup>() : null);
            AppendGrid(log, "WordBarGrid", word != null ? word.GetComponent<GridLayoutGroup>() : null);

            AppendImage(log, "TilePoolPanelDesignImage", FindNamedTransform(gameplay.transform, "TilePoolPanelDesign"));
            AppendImage(log, "TilePoolPanelDesignBackdropImage", FindNamedTransform(gameplay.transform, "TilePoolPanelDesign (1)"));

            Transform combatParent = word != null ? word.parent : null;
            if (combatParent != null)
            {
                log.AppendLine("-- Combat sibling order --");
                for (int i = 0; i < combatParent.childCount; i++)
                    log.AppendLine($"Sibling[{i}]={combatParent.GetChild(i).name}");
            }
        }
        else
        {
            log.AppendLine("GameplayCanvas=<missing>");
        }

        log.AppendLine("-- Canvas inventory --");
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            log.AppendLine(
                $"Canvas[{i}] path={BuildPath(canvas.transform)} root={canvas.isRootCanvas} mode={canvas.renderMode} " +
                $"enabled={canvas.enabled} scaleFactor={canvas.scaleFactor:0.###} localScale={canvas.transform.localScale} lossy={canvas.transform.lossyScale} " +
                (scaler == null
                    ? "scaler=<none>"
                    : $"scalerEnabled={scaler.enabled} mode={scaler.uiScaleMode} ref={scaler.referenceResolution} match={scaler.matchWidthOrHeight:0.###}"));
        }

        Debug.Log(log.ToString());
    }

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Log HUD Runtime Metrics _F6", true)]
    private static bool ValidateLogHudRuntimeMetrics() => Application.isPlaying;

    private static void AppendNamedRect(StringBuilder log, string label, Transform root, string objectName)
    {
        AppendRect(log, label, FindNamedTransform(root, objectName) as RectTransform);
    }

    private static void AppendRect(StringBuilder log, string label, RectTransform rect)
    {
        if (rect == null)
        {
            log.AppendLine($"{label}=<missing>");
            return;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        string canvasInfo = canvas == null
            ? "canvas=<none>"
            : $"canvas={canvas.name} mode={canvas.renderMode} scaleFactor={canvas.scaleFactor:0.###}";
        string scalerInfo = scaler == null
            ? "scaler=<none>"
            : $"scaler={scaler.uiScaleMode}/{scaler.screenMatchMode} ref={scaler.referenceResolution} match={scaler.matchWidthOrHeight:0.###}";

        log.AppendLine(
            $"{label}: path={BuildPath(rect)} rect={rect.rect} anchor=({rect.anchorMin}->{rect.anchorMax}) " +
            $"pos={rect.anchoredPosition} localScale={rect.localScale} lossy={rect.lossyScale} active={rect.gameObject.activeInHierarchy} " +
            $"worldBL={corners[0]} worldTR={corners[2]} {canvasInfo} {scalerInfo}");
    }

    private static void AppendGrid(StringBuilder log, string label, GridLayoutGroup grid)
    {
        if (grid == null)
        {
            log.AppendLine($"{label}=<missing>");
            return;
        }

        log.AppendLine(
            $"{label}: cell={grid.cellSize} spacing={grid.spacing} " +
            $"padding=({grid.padding.left},{grid.padding.right},{grid.padding.top},{grid.padding.bottom}) " +
            $"constraint={grid.constraint}/{grid.constraintCount} start={grid.startCorner}/{grid.startAxis} " +
            $"alignment={grid.childAlignment}");
    }

    private static void AppendImage(StringBuilder log, string label, Transform transform)
    {
        Image image = transform != null ? transform.GetComponent<Image>() : null;
        log.AppendLine(image == null
            ? $"{label}=<missing>"
            : $"{label}: raycastTarget={image.raycastTarget} enabled={image.enabled}");
    }

    private static Transform FindNamedTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    private static string BuildPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
#endif
