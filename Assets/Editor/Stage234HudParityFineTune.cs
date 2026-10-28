// Follow-up to the first guarded HUD art promotion. Makes the Stage 2-4
// existing serialized HUD match the real approved Stage 1 EDIT/PLAY
// composition. Only adjusts visuals and their canvas scaling.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class Stage234HudParityFineTune
{
    private const string Stage1 = "Assets/Scenes/SampleScene.unity";
    private const string Stage2 = "Assets/Scenes/Stage2Scene/Stage2Scene.unity";
    private const string Stage3 = "Assets/Scenes/Stage3Scene/Stage3Scene.unity";
    private const string Stage4 = "Assets/Scenes/Stage4Scene/Stage4Scene.unity";
    private const string Prefab = "Assets/Prefabs/PersistentGameplayHUD.prefab";
    private const string BackupFolder = "Library/GameplayHudEditModeParity_20261021/ArtScaleFineTuneBefore";
    private const string WordFrame = "WordBar_OrnateFrame";
    private const string Backdrop = "WordBar_OrnateBackdrop";

    [MenuItem("SPELLSTRIKE/HUD/Finalize Stage 2-4 EXACT Stage 1 Visual Proportions (Official)", priority = 236)]
    private static void FinalizeArt()
    {
        Scene active = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !active.IsValid() ||
            !active.isLoaded || active.isDirty || SceneManager.sceneCount != 1 ||
            EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
            Directory.Exists(BackupFolder))
        {
            Debug.LogError("[HUD Exact Parity] Exit Play, save current scene, choose Android and ensure fresh backup.");
            return;
        }
        Scene source = default, scene2 = default;
        GameObject prefab = null;
        try
        {
            foreach (string file in new[] { Stage1,Stage2,Stage3,Stage4,Prefab })
                if (!File.Exists(file))
                    throw new FileNotFoundException(file);
            source = active.path == Stage1 ? active :
                EditorSceneManager.OpenScene(Stage1, OpenSceneMode.Additive);
            if (source.isDirty) throw new InvalidOperationException("Source Stage 1 is dirty.");
            Transform approvedHud = Hud(source);
            Transform approvedWord = Named(approvedHud, "WordBarPanel");
            Transform approvedFrame = approvedWord.Find(WordFrame);
            Transform approvedBackdrop = approvedWord.Find(Backdrop);
            Transform approvedAttack = Named(approvedHud, "AttackButton");
            Transform approvedHealth = Named(approvedHud, "HealthBar");
            if (approvedFrame == null || approvedBackdrop == null ||
                approvedFrame.GetComponent<Image>()?.sprite == null ||
                approvedAttack.GetComponent<Image>()?.sprite == null)
                throw new InvalidOperationException("Stage 1 source WordBar/STRIKE art is incomplete.");
            if (approvedWord.GetComponent<GridLayoutGroup>() == null)
                throw new InvalidOperationException("Approved Stage1 Word Bar grid missing.");
            scene2 = EditorSceneManager.OpenScene(Stage2, OpenSceneMode.Additive);
            if (scene2.isDirty) throw new InvalidOperationException("Stage2 was dirty on open.");
            Transform secondHud = Hud(scene2);
            prefab = PrefabUtility.LoadPrefabContents(Prefab);
            Transform shared = prefab.transform;
            foreach (Transform hud in new[] { secondHud, shared })
            {
                if (Named(hud, "WordBarPanel").Find(WordFrame) == null ||
                    Named(hud, "WordBarPanel").Find(Backdrop) != null ||
                    Named(hud, "HealthBar").Find("SpellstrikePlayerStatusSkin") == null)
                    throw new InvalidOperationException("Unexpected already-finished or missing HUD elements: " + hud.name);
            }

            foreach (string file in new[] { Stage1,Stage2,Stage3,Stage4,Prefab })
            {
                string dest=BackupFolder+"/"+file;
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(file,dest,false);
            }
            Apply(approvedHud, secondHud);
            Apply(approvedHud, shared);
            PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);
            EditorSceneManager.MarkSceneDirty(scene2);
            if (!EditorSceneManager.SaveScene(scene2,Stage2))
                throw new IOException("Stage2 save failed.");
            Debug.Log("[HUD Exact Parity] Stage2 original HUD now has full-sized ornate Word Bar/STRIKE, stable backing and no old red player HP graphics.");
            EditorSceneManager.CloseScene(scene2,true);
            scene2=default;
            PrefabUtility.UnloadPrefabContents(prefab);
            prefab=null;

            foreach (string path in new[] { Stage3,Stage4 })
            {
                Scene loaded=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    if (loaded.isDirty) throw new InvalidOperationException(path+" is unexpectedly dirty.");
                    Transform hud=Hud(loaded);
                    RectTransform art = Named(hud,"WordBarPanel").Find(WordFrame) as RectTransform;
                    RectTransform srcArt=approvedFrame as RectTransform;
                    if (art == null || Named(hud,"WordBarPanel").Find(Backdrop)==null)
                        throw new InvalidOperationException(path+" missing Word Bar art.");
                    CopyRect(srcArt,art);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(art);
                    RectTransform attack=Named(hud,"AttackButton") as RectTransform;
                    CopyRect(approvedAttack as RectTransform,attack);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(attack);
                    RectTransform health=Named(hud,"HealthBar") as RectTransform;
                    CopyRect(approvedHealth as RectTransform,health);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(health);
                    EditorSceneManager.MarkSceneDirty(loaded);
                    if (!EditorSceneManager.SaveScene(loaded,path))
                        throw new IOException("Could not save "+path);
                    Debug.Log("[HUD Exact Parity] Saved "+path+" with Stage1 frame/STRIKE/HP Edit Mode transforms.");
                }
                finally
                {
                    if (loaded.IsValid() && loaded.isLoaded) EditorSceneManager.CloseScene(loaded,true);
                }
            }
            Debug.Log("[HUD Exact Parity] PASS: Stage2/3/4 finalized. Original Stage1 artist-authored artwork proportions and control owners preserved; mobile runtime now also respects approved ornate STRIKE local scale. Backup: "+BackupFolder);
            SceneView.RepaintAll();
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[HUD Exact Parity] STOPPED, possible partial saves. Do not rerun blindly; inspect dedicated backups and scene content.");
        }
        finally
        {
            if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
            if (scene2.IsValid() && scene2.isLoaded) EditorSceneManager.CloseScene(scene2,true);
            if (source.IsValid() && source.isLoaded && source != active) EditorSceneManager.CloseScene(source,true);
        }
    }

    private static Transform Hud(Scene scene)
    {
        var candidates=scene.GetRootGameObjects()
            .SelectMany(x=>x.GetComponentsInChildren<Transform>(true))
            .Where(x=>x.name=="PersistentHUDRoot").ToArray();
        if (candidates.Length != 1) throw new InvalidOperationException("Expected 1 HUD root in "+scene.path);
        return candidates[0];
    }

    private static Transform Named(Transform parent, string name)
    {
        var matches=parent.GetComponentsInChildren<Transform>(true)
            .Where(x=>x.name==name).ToArray();
        if (matches.Length!=1) throw new InvalidOperationException("Expected 1 "+name+" in "+parent.name);
        return matches[0];
    }

    private static void Apply(Transform source, Transform target)
    {
        RectTransform srcWord = Named(source,"WordBarPanel") as RectTransform;
        RectTransform dstWord = Named(target,"WordBarPanel") as RectTransform;
        RectTransform sourceFrame=srcWord.Find(WordFrame) as RectTransform;
        RectTransform existingFrame=dstWord.Find(WordFrame) as RectTransform;
        CopyRect(sourceFrame,existingFrame);
        var newBackdrop=UnityEngine.Object.Instantiate(srcWord.Find(Backdrop).gameObject,dstWord,false);
        newBackdrop.name=Backdrop;
        CopyRect(srcWord.Find(Backdrop) as RectTransform,newBackdrop.transform as RectTransform);
        newBackdrop.transform.SetSiblingIndex(0);
        existingFrame.SetSiblingIndex(1);
        RectTransform sourceAttack=Named(source,"AttackButton") as RectTransform;
        RectTransform destAttack=Named(target,"AttackButton") as RectTransform;
        CopyRect(sourceAttack,destAttack);

        RectTransform sourceHealth=Named(source,"HealthBar") as RectTransform;
        RectTransform destHealth=Named(target,"HealthBar") as RectTransform;
        CopyRect(sourceHealth,destHealth);
        CanvasScaler sourceHealthScaler=sourceHealth.GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();
        CanvasScaler targetHealthScaler=destHealth.GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();
        if (sourceHealthScaler != null && targetHealthScaler != null)
        {
            targetHealthScaler.uiScaleMode=sourceHealthScaler.uiScaleMode;
            targetHealthScaler.referenceResolution=sourceHealthScaler.referenceResolution;
            targetHealthScaler.screenMatchMode=sourceHealthScaler.screenMatchMode;
            targetHealthScaler.matchWidthOrHeight=sourceHealthScaler.matchWidthOrHeight;
        }

        // Mirror Stage1's editor state: the approved layered portrait, HP and XP
        // are the visible art; old health Image/heart remains in hierarchy for
        // existing Slider bindings but is not rendered a second time.
        Transform skin=destHealth.Find("SpellstrikePlayerStatusSkin");
        foreach(Graphic graphic in destHealth.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic==null || (skin != null && graphic.transform.IsChildOf(skin)))
                continue;
            graphic.enabled=false;
        }
        // Stage2's separately-authored enemy bar is inactive outside battle,
        // like Stage1 and the persistent runtime owner. It can be re-enabled
        // by the unchanged enemy encounter controller when needed.
        Transform authoredEnemy=target.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(x=>x.name=="EnemyHealthBar");
        if (authoredEnemy != null) authoredEnemy.gameObject.SetActive(false);

        if (dstWord.Find(WordFrame)?.GetComponent<Image>()?.sprite == null ||
            dstWord.Find(Backdrop)?.GetComponent<Image>()?.sprite == null ||
            Named(target,"AttackButton").GetComponent<Button>() == null ||
            Named(target,"HealthBar").GetComponent<Slider>() == null)
            throw new InvalidOperationException("Visual parity verification failed on "+target.name);
    }

    private static void CopyRect(RectTransform from, RectTransform to)
    {
        if (from == null || to == null) throw new InvalidOperationException("UI Rect missing");
        to.anchorMin=from.anchorMin;
        to.anchorMax=from.anchorMax;
        to.pivot=from.pivot;
        to.anchoredPosition=from.anchoredPosition;
        to.sizeDelta=from.sizeDelta;
        to.localScale=from.localScale;
        to.localRotation=from.localRotation;
    }
}
#endif
