#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time, visible Editor menu operation. Builds only a NEW isolated scene,
/// from the canonical exported study+archive model and detachable vault ceiling.
/// No Stage/gameplay scene, cinematic workbench, or Build Settings are edited.
/// </summary>
internal static class StudyArchiveCinematicSetBuilder
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/StudyArchive_CinematicSet_v01.unity";
    private const string BuildingPath =
        "Assets/Cinematics/Storyboard3D/Environment/LibraryShell/LibraryStudyArchive_OneBuilding_v01.glb";
    private const string RoofPath =
        "Assets/Cinematics/Storyboard3D/Environment/LibraryCeiling/Library_GothicVaultCeiling_v01.glb";
    private const string PrefabPath =
        "Assets/Cinematics/Storyboard3D/Environment/LibraryShell/StudyArchive_Environment_v01.prefab";
    private const string VanderPrefabPath =
        "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Prefabs/VanderPoel_CinematicActor.prefab";
    private const string CandlestickPath =
        "Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner/ScholarCandlestick_Unlit_v01.glb";

    [MenuItem("SPELLSTRIKE/Cinematics/Library/1 Build Isolated StudyArchive Set _F8")]
    private static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive] Enter Edit Mode to build cinematic scene.");
            return;
        }
        var prior = SceneManager.GetActiveScene();
        if (SceneManager.sceneCount != 1 || !prior.IsValid() || !prior.isLoaded ||
            prior.isDirty || string.IsNullOrEmpty(prior.path))
        {
            Debug.LogError("[StudyArchive] Save the currently loaded scene first. " +
                           "Exactly one clean scene must be open. No changes made.");
            return;
        }
        if (File.Exists(ScenePath) || File.Exists(PrefabPath))
        {
            Debug.LogError("[StudyArchive] Scene/prefab already exists. " +
                           "Refusing overwrite or duplicate.");
            return;
        }

        // Explicit refresh gives glTFast the opportunity to generate editor
        // prefab assets for .glb before referencing them in the new scene.
        AssetDatabase.ImportAsset(BuildingPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(RoofPath, ImportAssetOptions.ForceUpdate);
        var buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPath);
        var roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoofPath);
        if (buildingPrefab == null || roofPrefab == null)
        {
            Debug.LogError("[StudyArchive] glTFast could not import required GLBs " +
                           "as GameObject assets. Inspect these two GLB importers.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            var environment = new GameObject("STUDY_ARCHIVE_ONE_LIBRARY_ENVIRONMENT");
            var geo = (GameObject)PrefabUtility.InstantiatePrefab(buildingPrefab, scene);
            if (geo == null)
                throw new InvalidOperationException("Building prefab could not instantiate.");
            geo.name = "CONNECTED_STUDY_ARCHIVE__CINEMATIC_GEOMETRY";
            geo.transform.SetParent(environment.transform, false);
            geo.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            geo.transform.localScale = Vector3.one;

            var ceiling = (GameObject)PrefabUtility.InstantiatePrefab(roofPrefab, scene);
            if (ceiling == null)
                throw new InvalidOperationException("Ceiling prefab could not instantiate.");
            ceiling.name = "REMOVABLE_GOTHIC_CEILING__HIDE_BAYS_FOR_CAMERA";
            ceiling.transform.SetParent(environment.transform, false);
            ceiling.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            ceiling.transform.localScale = Vector3.one;

            // Keep each imported roof bay's transform visible in the hierarchy
            // so artists can hide single sections around their Timeline camera.
            var roofBays = ceiling.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("ROOF_STUDY_BAY_", StringComparison.Ordinal) ||
                            t.name.StartsWith("ROOF_ARCHIVE_BAY_", StringComparison.Ordinal))
                .Where(t => t.GetComponent<Renderer>() == null)
                .ToArray();
            if (roofBays.Length < 7)
                Debug.LogWarning("[StudyArchive] Expected 7 removable roof roots; " +
                                 "found " + roofBays.Length +
                                 ". Check imported GLB hierarchy before camera work.");

            var physics = new GameObject("SIMPLE_COLLIDERS__WALKABLE_FLOOR_AND_WALLS");
            physics.transform.SetParent(environment.transform, false);
            // Initial collision proxies, separate from complex bookcase geometry.
            // The canonical gateway at room divide remains OPEN.
            AddCollider(physics.transform,"Ground_14x20p5",
                new Vector3(0,-.10f,1.25f),new Vector3(14f,.20f,20.5f));
            AddCollider(physics.transform,"WestWall",
                new Vector3(-7f,2.75f,1.25f),new Vector3(.4f,5.5f,20.5f));
            AddCollider(physics.transform,"EastWall",
                new Vector3(7f,2.75f,1.25f),new Vector3(.4f,5.5f,20.5f));
            AddCollider(physics.transform,"NorthRearWall",
                new Vector3(0,2.75f,-9),new Vector3(14f,5.5f,.35f));
            AddCollider(physics.transform,"SouthStudyWall",
                new Vector3(0,2.75f,11.5f),new Vector3(14f,5.5f,.35f));
            AddCollider(physics.transform,"Divider_LeftOfGateway",
                new Vector3(-4.84f,2.75f,0),new Vector3(4.32f,5.5f,.4f));
            AddCollider(physics.transform,"Divider_RightOfGateway",
                new Vector3(4.84f,2.75f,0),new Vector3(4.32f,5.5f,.4f));

            // Light intentionally remains editable on a separate root. These
            // are staging-only and not claims of baked/A05s-optimized lighting.
            var lighting = new GameObject("CINEMATIC_PREVIEW_LIGHTING");
            AddPointLight(lighting.transform,"Study_Practical_Warm",
                new Vector3(0,4.4f,6.76f),new Color(1f,.69f,.36f),3.8f,11f);
            AddPointLight(lighting.transform,"Archive_Practical_Cool",
                new Vector3(0,4.3f,-4.5f),new Color(.38f,.61f,1f),2f,11f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.19f,.21f,.25f);

            var cameras = new GameObject("CINEMATIC_CAMERAS__ONE_ACTIVE");
            var camera = new GameObject("MAIN_CINEMATIC_CAMERA__STUDY_TO_ARCHIVE");
            camera.transform.SetParent(cameras.transform, false);
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(1.75f,2.45f,7.8f);
            camera.transform.rotation = Quaternion.LookRotation(
                new Vector3(0,2.45f,-1f)-camera.transform.position);
            var camComponent = camera.AddComponent<Camera>();
            camComponent.fieldOfView = 60f;
            camComponent.nearClipPlane = .12f;
            camComponent.farClipPlane = 110f;
            camera.AddComponent<AudioListener>();

            // A reference actor can be switched on for storyboard camera
            // framing, but stays INACTIVE until placement and animation review.
            var vander = AssetDatabase.LoadAssetAtPath<GameObject>(VanderPrefabPath);
            if (vander != null)
            {
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(vander, scene);
                actor.name = "VANDER_POEL_CINEMATIC_SCALE_TEST__DISABLED";
                actor.transform.position = new Vector3(0,0,4);
                actor.SetActive(false);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Environment prefab unexpectedly exists.");
            PrefabUtility.SaveAsPrefabAsset(environment, PrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Unable to save cinematic scene " + ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = environment;
            Debug.Log("[StudyArchive] SCENE BUILD PASS: " + ScenePath +
                      "; imported full library + seven detachable vault bays. " +
                      "Staging colliders, two lights, camera, optional disabled Vander actor. " +
                      "No gameplay scenes, Timeline or Build Settings changed.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[StudyArchive] Build incomplete. Inspect isolated scene " +
                           "and created assets before rerunning. Original scenes untouched.");
        }
    }

    private static void AddCollider(Transform parent,string name,Vector3 position,Vector3 size)
    {
        var node = new GameObject(name);
        node.transform.SetParent(parent,false);
        node.transform.localPosition=position;
        var collider=node.AddComponent<BoxCollider>();
        collider.size=size;
    }

    private static void AddPointLight(Transform parent,string name,Vector3 position,
                                      Color color,float intensity,float range)
    {
        var node=new GameObject(name);
        node.transform.SetParent(parent,false);
        node.transform.position=position;
        var light=node.AddComponent<Light>();
        light.type=LightType.Point;
        light.color=color;
        light.intensity=intensity;
        light.range=range;
        light.shadows=LightShadows.None;
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/2 Toggle All Roof Bays")]
    private static void ToggleRoof()
    {
        if (SceneManager.GetActiveScene().path!=ScenePath ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive] Open the isolated cinematic scene in Edit Mode.");
            return;
        }
        var roof=GameObject.Find("REMOVABLE_GOTHIC_CEILING__HIDE_BAYS_FOR_CAMERA");
        if (roof==null) {Debug.LogError("[StudyArchive] Missing ceiling.");return;}
        Undo.RecordObject(roof,"Toggle cinematic study/archive roof");
        roof.SetActive(!roof.activeSelf);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[StudyArchive] Roof now " + (roof.activeSelf?"visible":"hidden") +
                  ". Use Ctrl+S to save the layout change.");
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/3 Align Scene to Imported Blender Axes")]
    private static void AlignExistingScene()
    {
        var scene=SceneManager.GetActiveScene();
        if (scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive] Only the isolated StudyArchive scene in Edit Mode can be aligned.");
            return;
        }
        // Blender +Y is converted by the glTF pipeline to Unity -Z;
        // study is therefore positive-Z and archive negative-Z.
        Transform FindInScene(string name)
        {
            var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true));
            return all.FirstOrDefault(x=>x.name==name);
        }
        void Move(string name,Vector3 position)
        {
            var item=FindInScene(name);
            if (item==null) throw new InvalidOperationException("Missing object: "+name);
            Undo.RecordObject(item,"Align StudyArchive "+name);
            item.position=position;
        }
        try
        {
            Move("Ground_14x20p5",new Vector3(0,-.10f,1.25f));
            Move("WestWall",new Vector3(-7,2.75f,1.25f));
            Move("EastWall",new Vector3(7,2.75f,1.25f));
            Move("NorthRearWall",new Vector3(0,2.75f,-9));
            Move("SouthStudyWall",new Vector3(0,2.75f,11.5f));
            Move("Study_Practical_Warm",new Vector3(0,4.4f,6.76f));
            Move("Archive_Practical_Cool",new Vector3(0,4.3f,-4.5f));
            var studyLight=FindInScene("Study_Practical_Warm").GetComponent<Light>();
            Undo.RecordObject(studyLight,"Brighten cinematic study practical");
            studyLight.intensity=3.8f;
            if (FindInScene("VANDER_POEL_CINEMATIC_SCALE_TEST__DISABLED")!=null)
                Move("VANDER_POEL_CINEMATIC_SCALE_TEST__DISABLED",new Vector3(0,0,4));
            var camera=FindInScene("MAIN_CINEMATIC_CAMERA__STUDY_TO_ARCHIVE");
            Undo.RecordObject(camera,"Correct cinematic camera facing direction");
            camera.position=new Vector3(1.75f,2.45f,7.8f);
            camera.rotation=Quaternion.LookRotation(new Vector3(0,2.45f,-1)-camera.position);

            var environment=FindInScene("STUDY_ARCHIVE_ONE_LIBRARY_ENVIRONMENT");
            if (environment==null)
                throw new InvalidOperationException("Environment prefab root missing.");
            PrefabUtility.SaveAsPrefabAsset(environment.gameObject,PrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("Could not save aligned scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("[StudyArchive] AXIS ALIGNMENT PASS: warm study at +Z, " +
                      "cool archive at -Z, camera now on study side. Colliders and prefab synchronized.");
        }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/4 Finish StudyArchive Dressing and Camera Presets")]
    private static void FinishDressing()
    {
        var scene=SceneManager.GetActiveScene();
        if (scene.path!=ScenePath || scene.isDirty ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive] Save the isolated scene in Edit Mode " +
                           "before adding the final dressing. No changes made.");
            return;
        }
        if (GameObject.Find("FINAL_STUDY_ARCHIVE_SET_DRESSING") != null)
        {
            Debug.LogError("[StudyArchive] Dressing already exists; refusing duplicates.");
            return;
        }
        AssetDatabase.ImportAsset(CandlestickPath,ImportAssetOptions.ForceUpdate);
        var prop=AssetDatabase.LoadAssetAtPath<GameObject>(CandlestickPath);
        if (prop==null)
        {
            Debug.LogError("[StudyArchive] Cannot import independent brass candlestick GLB.");
            return;
        }
        try
        {
            var root=new GameObject("FINAL_STUDY_ARCHIVE_SET_DRESSING");
            var candle=(GameObject)PrefabUtility.InstantiatePrefab(prop,scene);
            candle.name="SCHOLAR_TABLE_LEFT_CANDLESTICK__UNLIT";
            candle.transform.SetParent(root.transform,true);
            candle.transform.position=new Vector3(-5.27f,.85f,7.32f);
            candle.transform.rotation=Quaternion.identity;
            candle.transform.localScale=Vector3.one;

            // Very gentle non-shadow-casting fill improves indoor material
            // legibility in PC/Game view without eight realtime candle lights.
            var fill=new GameObject("INTERIOR_SKYLIGHT_PREVIEW_NO_SHADOWS");
            fill.transform.SetParent(root.transform,false);
            fill.transform.rotation=Quaternion.Euler(48f,-28f,0f);
            var light=fill.AddComponent<Light>();
            light.type=LightType.Directional;
            light.color=new Color(.65f,.74f,.88f);
            light.intensity=.49f;
            light.shadows=LightShadows.None;

            var presets=new GameObject("EXTRA_CINEMATIC_SHOT_PRESETS__DISABLED");
            presets.transform.SetParent(root.transform,false);
            CreateDisabledCamera(presets.transform,"Shot02_ScholarTable",
                new Vector3(-3.5f,1.85f,8.9f),new Vector3(-5.3f,1.4f,7.65f),44f);
            CreateDisabledCamera(presets.transform,"Shot03_FrontStainedGlass",
                new Vector3(0,2.25f,3.4f),new Vector3(0,2.55f,11.5f),54f);
            CreateDisabledCamera(presets.transform,"Shot04_ArchiveFacingStudy",
                new Vector3(1.3f,2.4f,-6.6f),new Vector3(0,2.4f,3.8f),58f);
            CreateDisabledCamera(presets.transform,"Shot05_StudyVaultInterior",
                new Vector3(3f,2.1f,6.1f),new Vector3(0,6.6f,6.1f),66f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("StudyArchive dressing scene could not save.");
            AssetDatabase.SaveAssets();
            Debug.Log("[StudyArchive] DRESSING PASS: standalone unlit brass " +
                      "candlestick, soft skylight, 4 disabled shot cameras added. " +
                      "No source furniture mesh, original workbench, or gameplay scene edited.");
        }
        catch (Exception e) {Debug.LogException(e);}
    }

    private static void CreateDisabledCamera(Transform parent,string name,
                                             Vector3 position,Vector3 target,float fov)
    {
        var gameObject=new GameObject(name);
        gameObject.transform.SetParent(parent,false);
        gameObject.transform.position=position;
        gameObject.transform.rotation=Quaternion.LookRotation(target-position);
        var cam=gameObject.AddComponent<Camera>();
        cam.fieldOfView=fov;
        cam.nearClipPlane=.15f;
        cam.farClipPlane=130f;
        gameObject.SetActive(false);
    }

    [Serializable]
    private class SceneAudit
    {
        public string scene;
        public int rendererCount;
        public int triangleCount;
        public int roofSections;
        public int roofGables;
        public int colliderCount;
        public int cameraCount;
        public int enabledCameras;
        public int missingMaterialSlots;
        public int missingScripts;
        public bool candleOnStudyTable;
        public bool importedBuilding;
        public bool importedVault;
        public bool previewOnlyNoBuildSettings;
        public bool passed;
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/5 Audit Unity StudyArchive Set")]
    private static void AuditSet()
    {
        var scene=SceneManager.GetActiveScene();
        if (scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive QA] Open only the isolated cinematic scene in Edit Mode.");
            return;
        }
        var roots=scene.GetRootGameObjects();
        var transforms=roots.SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var gameObjects=transforms.Select(x=>x.gameObject).ToArray();
        var renderers=roots.SelectMany(x=>x.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        var filters=roots.SelectMany(x=>x.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        var cameras=roots.SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).ToArray();
        var expectedBays=Enumerable.Range(1,4).Select(i=>$"ROOF_STUDY_BAY_{i:00}")
            .Concat(Enumerable.Range(1,3).Select(i=>$"ROOF_ARCHIVE_BAY_{i:00}"))
            .ToArray();
        var audit=new SceneAudit
        {
            scene=scene.path,
            rendererCount=renderers.Length,
            triangleCount=filters.Where(x=>x.sharedMesh!=null)
                .Sum(x=>x.sharedMesh.triangles.Length/3),
            roofSections=transforms.Count(x=>expectedBays.Contains(x.name)),
            roofGables=transforms.Count(x=>
                x.name.StartsWith("ROOF_GABLE_",StringComparison.Ordinal) &&
                x.GetComponent<Renderer>()==null),
            colliderCount=roots.Sum(x=>x.GetComponentsInChildren<BoxCollider>(true).Length),
            cameraCount=cameras.Length,
            enabledCameras=cameras.Count(x=>x.enabled && x.gameObject.activeInHierarchy),
            missingMaterialSlots=renderers.Sum(x=>
                x.sharedMaterials.Count(m=>m==null || m.shader==null)),
            missingScripts=gameObjects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),
            candleOnStudyTable=transforms.Any(x=>
                x.name=="SCHOLAR_TABLE_LEFT_CANDLESTICK__UNLIT"),
            importedBuilding=AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPath)!=null,
            importedVault=AssetDatabase.LoadAssetAtPath<GameObject>(RoofPath)!=null,
            previewOnlyNoBuildSettings=!EditorBuildSettings.scenes.Any(x=>x.path==ScenePath),
        };
        audit.passed=audit.rendererCount>250 && audit.triangleCount>200000 &&
                     audit.roofSections==7 && audit.roofGables==3 &&
                     audit.colliderCount>=7 && audit.cameraCount>=5 &&
                     audit.enabledCameras==1 && audit.missingMaterialSlots==0 &&
                     audit.missingScripts==0 && audit.candleOnStudyTable &&
                     audit.importedBuilding && audit.importedVault &&
                     audit.previewOnlyNoBuildSettings;
        var output=Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "Library/StoryboardLibraryCeiling_20261023/UnityStudyArchive_SetAudit.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output,JsonUtility.ToJson(audit,true));
        if (audit.passed)
            Debug.Log("[StudyArchive QA] PASS: "+audit.rendererCount+" renderers, "+
                      audit.triangleCount+" triangles, 7 roof bays, 3 gables, "+
                      audit.colliderCount+" colliders, 5 camera presets; report "+output);
        else
            Debug.LogError("[StudyArchive QA] FAILED checks: "+JsonUtility.ToJson(audit)+
                           " inspect "+output);
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/6 Preview Scholar Table Camera")]
    private static void PreviewScholarCamera()
    {
        SetCameraPreview("Shot02_ScholarTable",false);
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/7 Restore Main Cinematic Camera")]
    private static void RestoreMainCamera()
    {
        SetCameraPreview("MAIN_CINEMATIC_CAMERA__STUDY_TO_ARCHIVE",true);
    }

    private static void SetCameraPreview(string cameraName,bool persist)
    {
        var scene=SceneManager.GetActiveScene();
        if (scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive Cameras] Must be in isolated scene Edit Mode.");
            return;
        }
        var cameras=scene.GetRootGameObjects()
            .SelectMany(x=>x.GetComponentsInChildren<Camera>(true)).ToArray();
        var selected=cameras.SingleOrDefault(x=>x.name==cameraName);
        if (selected==null || cameras.Length<5)
        {
            Debug.LogError("[StudyArchive Cameras] Missing camera preset: "+cameraName);
            return;
        }
        foreach(var cam in cameras)
        {
            Undo.RecordObject(cam.gameObject,"Preview cinematic shot");
            cam.gameObject.SetActive(cam==selected);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (persist && !EditorSceneManager.SaveScene(scene,ScenePath))
            Debug.LogError("[StudyArchive Cameras] Failed to save restored default view.");
        Debug.Log("[StudyArchive Cameras] "+(persist?"MAIN CAMERA RESTORED":"SCHOLAR CAMERA PREVIEW")+
                  ": "+selected.name);
    }

    [MenuItem("SPELLSTRIKE/Cinematics/Library/8 Reframe Scholar Table Camera")]
    private static void ReframeScholarTable()
    {
        var scene=SceneManager.GetActiveScene();
        if (scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[StudyArchive Cameras] Isolated scene Edit Mode only.");
            return;
        }
        var cam=scene.GetRootGameObjects()
            .SelectMany(x=>x.GetComponentsInChildren<Camera>(true))
            .SingleOrDefault(x=>x.name=="Shot02_ScholarTable");
        if(cam==null){Debug.LogError("[StudyArchive Cameras] Scholar camera missing.");return;}
        Undo.RecordObject(cam.transform,"Reframe scholar side table shot");
        Undo.RecordObject(cam,"Reframe scholar side table shot");
        var pos=new Vector3(-3.5f,1.85f,8.9f);
        cam.transform.position=pos;
        cam.transform.rotation=Quaternion.LookRotation(new Vector3(-5.3f,1.4f,7.65f)-pos);
        cam.fieldOfView=44f;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[StudyArchive Cameras] Scholar close-up now frames the EAST " +
                  "study wall's antique globe and separate candle holder.");
    }
}
#endif
