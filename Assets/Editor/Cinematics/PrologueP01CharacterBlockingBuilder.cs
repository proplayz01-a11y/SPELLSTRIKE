#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guarded one-time assembly of the two standalone Blender prototype
/// character rigs into the isolated family-home scene. Old Van/production
/// gameplay and other scenes are never modified.
/// </summary>
internal static class PrologueP01CharacterBlockingBuilder
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_Defense_v01.unity";
    private const string CharDir =
        "Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes/";
    private const string OldSceneBackup =
        "Library/StoryboardDefenseCharacters_20261023/P01_BEFORE_CharacterPlacement.unity";
    private const string RootName = "P01_DEFENSE_CHARACTER_BLOCKING__REPLACE_WITH_FINAL_ACTORS";

    [MenuItem("SPELLSTRIKE/Cinematics/Defense Prototype/3 Place Young Van and Nevali")]
    private static void Place()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || !scene.isLoaded ||
            SceneManager.sceneCount != 1 || scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[P01 Characters] Open and SAVE ONLY the Family Home " +
                           "cinematic scene in Edit Mode. No changes made.");
            return;
        }
        var all=scene.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        if (all.Any(t=>t.name==RootName) || File.Exists(OldSceneBackup))
        {
            Debug.LogError("[P01 Characters] Prototype actors or backup already exist. " +
                           "Refusing to overwrite a previous placement.");
            return;
        }
        var youngPath=CharDir+"YoungVanAert_Prototype_v01.glb";
        var nevaliPath=CharDir+"Nevali_Prototype_v01.glb";
        AssetDatabase.ImportAsset(youngPath,ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(nevaliPath,ImportAssetOptions.ForceUpdate);
        GameObject young=AssetDatabase.LoadAssetAtPath<GameObject>(youngPath);
        GameObject nevali=AssetDatabase.LoadAssetAtPath<GameObject>(nevaliPath);
        if (young==null || nevali==null)
        {
            Debug.LogError("[P01 Characters] One or both Blender rigged GLBs " +
                           "could not import as Unity GameObject assets.");
            return;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OldSceneBackup));
            File.Copy(ScenePath,OldSceneBackup,false);
            var root=new GameObject(RootName);
            var vanGo=(GameObject)PrefabUtility.InstantiatePrefab(young,scene);
            var girlGo=(GameObject)PrefabUtility.InstantiatePrefab(nevali,scene);
            if(vanGo==null || girlGo==null)
                throw new InvalidOperationException("Could not create actor instances.");
            vanGo.name="YOUNG_VAN_AERT__CINEMATIC_ONLY";
            vanGo.transform.SetParent(root.transform,true);
            // Based on Panel 01's actual family-home camera: his silhouette
            // sits in the left doorway foreground watching Nevali.
            vanGo.transform.position=new Vector3(-2.65f,0,4.27f);
            vanGo.transform.rotation=Quaternion.Euler(0,178f,0);
            vanGo.transform.localScale=Vector3.one;
            girlGo.name="NEVALI_CHILD__CINEMATIC_ONLY";
            girlGo.transform.SetParent(root.transform,true);
            girlGo.transform.position=new Vector3(1.28f,0,.45f);
            girlGo.transform.rotation=Quaternion.Euler(0,5f,0);
            girlGo.transform.localScale=Vector3.one;
            var youngRenderer=vanGo.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var childRenderer=girlGo.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(youngRenderer.Length==0 || childRenderer.Length==0)
                Debug.LogWarning("[P01 Characters] Rig skin renderers not automatically exposed " +
                                 "by glTF importer; inspect actor hierarchy before motion retarget.");
            foreach(var prefabRoot in new []{vanGo,girlGo})
            {
                foreach(var camera in prefabRoot.GetComponentsInChildren<Camera>(true))
                    camera.enabled=false;
                foreach(var listener in prefabRoot.GetComponentsInChildren<AudioListener>(true))
                    listener.enabled=false;
                foreach(var rigidbody in prefabRoot.GetComponentsInChildren<Rigidbody>(true))
                {
                    rigidbody.isKinematic=true;
                    rigidbody.detectCollisions=false;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("Could not save P01 with new cinematic actors.");
            Debug.Log("[P01 Characters] CHARACTER PLACEMENT PASS: " +
                      "Young Van + child Nevali model instances added to family home; " +
                      "skinned renderers="+youngRenderer.Length+"/"+childRenderer.Length+
                      ". Source GLBs are independent of Older Van; " +
                      "backup "+OldSceneBackup);
            Selection.activeGameObject=root;
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[P01 Characters] Scene placement interrupted. " +
                           "Inspect backup and hierarchy before any retry.");
        }
    }
}
#endif
