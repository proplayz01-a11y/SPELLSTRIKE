using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a new isolated Main Menu V11 from protected V10.
/// Replaces only the five A-E orbit cubes with optimized shared Rodin
/// bronze/silver/gold bases and independent black 3D letter meshes.
/// Existing portal, Van, book, staff, UI and RenderTexture cameras are retained.
/// </summary>
public static class MainMenuV11RodinBuilder
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v10_BookStaffPolish.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261031_v11_RodinFinal.unity";

    private const string BaseFolder = "Assets/MenuV2/RodinV11/SharedBases/";
    private const string LetterFolder = "Assets/MenuV2/RodinV11/BlackLetters/";

    private const int BackLayer = 28;
    private const int FrontLayer = 29;

    // The source Rodin model originates at the tile's lower edge, with
    // its visual center at y=0.883. Center before making the tile orbit.
    private const float TileScale = .67f;
    private const float LocalVisualCenterY = .883f;

    [MenuItem("SPELLSTRIKE/Main Menu V11/Build Isolated Rodin Final Review")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName(
                "VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[Main Menu V11] VALORANT is running. No changes.");
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isUpdating || EditorApplication.isCompiling)
        {
            Debug.LogWarning("[Main Menu V11] Editor must be idle and out of Play Mode.");
            return;
        }

        Scene source = SceneManager.GetActiveScene();
        if (source.path != Source || source.isDirty)
        {
            Debug.LogError("[Main Menu V11] Open the clean protected V10 source first. " +
                           "active=" + source.path + " dirty=" + source.isDirty);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[Main Menu V11] Destination already exists; refusing overwrite.");
            return;
        }

        string letters = "WORDS";
        string[] rarities = { "Silver", "Bronze", "Bronze", "Bronze", "Bronze" };

        // Validate all 26 glyphs so our final menu project has an imported
        // alphabet. Qu replaces Q; no duplicate full tile materials.
        foreach (char letter in "ABCDEFGHIJKLMNOPRSTUVWXYZ")
        {
            if (LoadLetter(letter.ToString()) == null)
                throw new InvalidOperationException(
                    "Missing texture-free letter asset: " + letter);
        }
        if (LoadLetter("Qu") == null)
            throw new InvalidOperationException("Missing gold Qu glyph.");

        foreach (string tier in new[] { "Bronze", "Silver", "Gold" })
        {
            if (LoadBase(tier) == null)
                throw new InvalidOperationException("Missing shared tile base: " + tier);
        }

        GameObject orbitOriginal = GameObject.Find("FiveGameTileOrbitRig");
        if (orbitOriginal == null || orbitOriginal.transform.childCount != 5 ||
            GameObject.Find("Orbit_Back_Camera") == null ||
            GameObject.Find("Orbit_Front_Camera") == null ||
            GameObject.Find("ApprovedOlderVanAert") == null)
        {
            throw new InvalidOperationException(
                "Protected V10 orbit/compositing rig is not in expected state.");
        }

        if (!EditorSceneManager.SaveScene(source, Destination, true))
            throw new InvalidOperationException(
                "Could not create a safe independent V11 scene copy.");

        Scene v11 = EditorSceneManager.OpenScene(
            Destination, OpenSceneMode.Single);
        Transform rig = GameObject.Find("FiveGameTileOrbitRig").transform;
        rig.gameObject.name = "FiveGameTileOrbitRig_V11_Rodin";

        // Only destroy old A-E menu-orbit replicas *within the new V11 scene*.
        for (int child = rig.childCount - 1; child >= 0; --child)
        {
            Transform original = rig.GetChild(child);
            if (original.name != "MenuOrbit_" + "ABCDE"[child])
                throw new InvalidOperationException(
                    "Unexpected V10 orbit child at index " + child + ": " +
                    original.name + ". V11 scene needs manual inspection.");
            UnityEngine.Object.DestroyImmediate(original.gameObject);
        }

        for (int i = 0; i < letters.Length; ++i)
        {
            string glyph = letters[i].ToString();
            string rarity = rarities[i];

            GameObject slot = new GameObject("MenuOrbit_" + glyph + "_V11");
            slot.transform.SetParent(rig, false);
            slot.transform.localScale = Vector3.one;

            Transform visual = new GameObject("CenteredRodinTile_" + glyph)
                .transform;
            visual.SetParent(slot.transform, false);
            visual.localPosition = new Vector3(
                0f, -LocalVisualCenterY * TileScale, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one * TileScale;

            GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                LoadBase(rarity), visual);
            GameObject lettersInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                LoadLetter(glyph), visual);
            if (baseInstance == null || lettersInstance == null)
                throw new InvalidOperationException(
                    "Could not instantiate Rodin base or letter " + glyph);

            baseInstance.name = "SharedBase_" + rarity;
            lettersInstance.name = "Black3DLetter_" + glyph + "_BothFaces";
            Reset(baseInstance.transform);
            Reset(lettersInstance.transform);

            // Orbital UI world has its own lighting and overlay cameras.
            // Unnecessary shadow passes on mobile are disabled on these props.
            foreach (Renderer renderer in
                slot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Collider c in slot.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            MenuV11RodinOrbitTile controller =
                slot.AddComponent<MenuV11RodinOrbitTile>();
            controller.Configure(i * Mathf.PI * 2f / letters.Length,
                                 .31f, BackLayer, FrontLayer);
            if (slot.GetComponentsInChildren<MeshRenderer>(true).Length != 3)
                throw new InvalidOperationException(
                    "Expected one Rodin body + 2 black letters for " + glyph);
        }

        if (rig.childCount != 5)
            throw new InvalidOperationException("Expected exactly 5 V11 orbit tiles.");

        EditorSceneManager.MarkSceneDirty(v11);
        if (!EditorSceneManager.SaveScene(v11))
            throw new InvalidOperationException("Failed to save isolated V11 scene.");

        AssetDatabase.SaveAssets();
        Debug.Log("[Main Menu V11] Saved isolated Rodin WORDS main menu. " +
                  "5 orbit slots, 3 shared rarity bases, true black 3D text " +
                  "front/back, original V10 untouched. Scene=" + Destination);
    }

    private static GameObject LoadBase(string rarity)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(
            BaseFolder + "SPELLSTRIKE_" + rarity + "_SharedBase_1024.glb");
    }

    private static GameObject LoadLetter(string letter)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(
            LetterFolder + "SPELLSTRIKE_" + letter +
            "_Black3D_BothFaces_MobileLean.glb");
    }

    private static void Reset(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }
}
