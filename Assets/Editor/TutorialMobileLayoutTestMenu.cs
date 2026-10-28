#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode QA shortcuts for the responsive Tutorial combat HUD.
/// F8 previews PLAY in the Word Bar while leaving the remaining Tile Pool visible.
/// F7 previews a full 16-tile 8x2 Tile Pool. No save data is mutated.
/// </summary>
public static class TutorialMobileLayoutTestMenu
{
    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Preview STRIKE Layout _F8")]
    private static void PreviewStrikeLayout()
    {
        if (!TryGetTutorial(out TutorialDirector director, out TileManager tileManager))
            return;

        HashSet<int> preExistingTileIds = CapturePoolTileInstanceIds(tileManager);

        tileManager.ClearTileSelectionFilter();
        if (!tileManager.PrepareGuidedWordPool("PLAY", 16))
        {
            Debug.LogError("[TutorialLayoutTest] Could not prepare the PLAY tile pool.");
            return;
        }

        foreach (char requiredLetter in "PLAY")
        {
            Tile matchingTile = FindPoolTile(tileManager, requiredLetter, preExistingTileIds);
            if (matchingTile == null)
            {
                Debug.LogError($"[TutorialLayoutTest] Missing guided tile '{requiredLetter}'.");
                return;
            }

            tileManager.ToggleTilePanel(matchingTile);
        }

        SetTutorialStep(director, TutorialDirector.TutorialStep.StrikePlay);
        RefreshResponsiveLayout();
        Debug.Log("[TutorialLayoutTest] F8: PLAY formed with the remaining pool visible. Save data was not changed.");
    }

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Preview STRIKE Layout _F8", true)]
    private static bool ValidatePreviewStrikeLayout() => Application.isPlaying;

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Preview Full Pool Layout _F7")]
    private static void PreviewFullPoolLayout()
    {
        if (!TryGetTutorial(out TutorialDirector director, out TileManager tileManager))
            return;

        tileManager.ClearTileSelectionFilter();
        if (!tileManager.PrepareGuidedWordPool("PLAY", 16))
        {
            Debug.LogError("[TutorialLayoutTest] Could not prepare the 16-tile pool.");
            return;
        }

        SetTutorialStep(director, TutorialDirector.TutorialStep.FormPlay);
        RefreshResponsiveLayout();
        Debug.Log("[TutorialLayoutTest] F7: full 16-tile 8x2 Tile Pool preview. Save data was not changed.");
    }

    [MenuItem("SPELLSTRIKE/Testing/Tutorial/Preview Full Pool Layout _F7", true)]
    private static bool ValidatePreviewFullPoolLayout() => Application.isPlaying;

    private static bool TryGetTutorial(out TutorialDirector director, out TileManager tileManager)
    {
        director = Object.FindFirstObjectByType<TutorialDirector>();
        tileManager = Object.FindFirstObjectByType<TileManager>();
        if (!Application.isPlaying || director == null || tileManager == null || tileManager.tilePoolPanel == null)
        {
            Debug.LogError("[TutorialLayoutTest] Enter Tutorial Play Mode before running the layout preview.");
            return false;
        }

        return true;
    }

    private static void SetTutorialStep(TutorialDirector director, TutorialDirector.TutorialStep step)
    {
        MethodInfo setStep = typeof(TutorialDirector).GetMethod(
            "SetStep",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (setStep == null)
        {
            Debug.LogError("[TutorialLayoutTest] Could not resolve TutorialDirector.SetStep.");
            return;
        }

        setStep.Invoke(director, new object[] { step });
    }

    private static void RefreshResponsiveLayout()
    {
        TutorialResponsiveLayout layout = Object.FindFirstObjectByType<TutorialResponsiveLayout>();
        layout?.ForceRefresh();
    }

    private static HashSet<int> CapturePoolTileInstanceIds(TileManager tileManager)
    {
        HashSet<int> ids = new HashSet<int>();
        if (tileManager?.tilePoolPanel == null)
            return ids;

        for (int i = 0; i < tileManager.tilePoolPanel.childCount; i++)
        {
            Tile tile = tileManager.tilePoolPanel.GetChild(i).GetComponent<Tile>();
            if (tile != null)
                ids.Add(tile.GetInstanceID());
        }

        return ids;
    }

    private static Tile FindPoolTile(
        TileManager tileManager,
        char requiredLetter,
        HashSet<int> ignoredInstanceIds = null)
    {
        for (int i = 0; i < tileManager.tilePoolPanel.childCount; i++)
        {
            Tile tile = tileManager.tilePoolPanel.GetChild(i).GetComponent<Tile>();
            if (tile != null
                && (ignoredInstanceIds == null || !ignoredInstanceIds.Contains(tile.GetInstanceID()))
                && tile.gameObject.activeInHierarchy
                && char.ToUpperInvariant(tile.letter) == requiredLetter)
            {
                return tile;
            }
        }

        return null;
    }
}
#endif

