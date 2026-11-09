using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Non-destructive follow-up to recorded Menu V3 QA: restore the stronger
/// original warm-gold EXTERIOR glow without changing normal/hover silhouettes
/// or illuminating the opening occupied by the independently animated vortex.
/// </summary>
public static class MainMenuV2AdventureGlowV04
{
    public const string Source =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v03_QA.unity";
    public const string Destination =
        "Assets/Scenes/MenuConcepts/MainMenuV2_Prototype_20261030_v04_AdventureGold.unity";
    private const string HoverSprite =
        "Assets/MenuV2/Art/Adventure_Arch_Hover_OuterGold_V04.png";

    [MenuItem("SPELLSTRIKE/Main Menu V2/Restore Adventure Outer Glow (V04)")]
    public static void Build()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
        {
            Debug.LogWarning("[MenuV2] V04 blocked because VALORANT is running.");
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[MenuV2] V04 blocked during Play Mode, import, or compilation.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != Source || scene.isDirty)
        {
            Debug.LogWarning("[MenuV2] Open and save V03 before building V04: " + scene.path);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination) != null)
        {
            Debug.LogWarning("[MenuV2] V04 already exists; refusing to overwrite.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(HoverSprite) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[MenuV2] Gold outer-glow art missing: " + HoverSprite);
            return;
        }
        if (importer.textureType != TextureImporterType.Sprite ||
            importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HoverSprite);
        if (sprite == null)
        {
            Debug.LogError("[MenuV2] V04 art failed to import as Sprite.");
            return;
        }
        if (!EditorSceneManager.SaveScene(scene, Destination, true))
        {
            Debug.LogError("[MenuV2] Could not create an independent V04 copy.");
            return;
        }

        Scene v04 = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Single);
        Transform canvas = GameObject.Find("MainMenuV2_Canvas").transform;
        Transform adventure = canvas.Find("AdventureGateway");
        Image hover = adventure.Find("Hover_GoldOutline").GetComponent<Image>();
        hover.sprite = sprite;
        hover.preserveAspect = true;
        hover.raycastTarget = false;

        // Previous alignment pass intentionally lowered intensity. Only this
        // target returns to a stronger reference-level 96% hover blend.
        MenuV2HoverTarget target =
            adventure.GetComponentInChildren<MenuV2HoverTarget>(true);
        SerializedObject so = new SerializedObject(target);
        so.FindProperty("hoverOpacity").floatValue = .96f;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(v04);
        EditorSceneManager.SaveScene(v04);
        Debug.Log("[MenuV2] V04: restored strong exterior-only Adventure glow at " +
                  Destination + "; V03 and original menu untouched.");
    }
}
