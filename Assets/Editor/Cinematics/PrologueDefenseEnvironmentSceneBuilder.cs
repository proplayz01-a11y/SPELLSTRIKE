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
/// Visible, one-time Editor action for three low-cost narrated cinematic
/// environment prototypes. Uses native Blender exports imported by glTFast.
/// Never edits gameplay scenes, StoryScene, existing StudyArchive or Build Settings.
/// </summary>
internal static class PrologueDefenseEnvironmentSceneBuilder
{
    private const string AssetRoot =
        "Assets/Cinematics/Storyboard3D/Environment/DefenseSets/";
    private const string SceneRoot = "Assets/Scenes/Cinematics/";

    private struct SetSpec
    {
        public string name;
        public string glb;
        public string scene;
        public string panel;
        public Vector3 camPosition;
        public Vector3 camTarget;
        public float fov;
        public Color ambient;
        public Color keyColor;
        public float keyIntensity;
        public Vector3 keyPosition;
        public float keyRange;
        public Vector3 groundSize;
        public Vector3 groundCenter;
    }

    private static readonly SetSpec[] Sets =
    {
        new SetSpec
        {
            name="Family Home - Peaceful",
            glb=AssetRoot+"Prologue_Home_01_Peaceful_v01.glb",
            scene=SceneRoot+"Prologue_P01_FamilyHome_Defense_v01.unity",
            panel="PANEL_01_DOORWAY_VIEW__VAN_WATCHES_NEVALI_PLAY",
            camPosition=new Vector3(-2.45f,2.12f,5.20f),
            camTarget=new Vector3(1.25f,1.20f,.42f),
            fov=46f,
            ambient=new Color(.24f,.20f,.16f),
            keyColor=new Color(1f,.74f,.49f),
            keyIntensity=4.5f,
            keyPosition=new Vector3(2f,3.45f,-.4f),
            keyRange=10,
            groundCenter=new Vector3(0,-.12f,0),
            groundSize=new Vector3(10f,.2f,12f)
        },
        new SetSpec
        {
            name="Great War - Ruined City",
            glb=AssetRoot+"Prologue_GreatWar_02_03_v01.glb",
            scene=SceneRoot+"Prologue_P02_P03_GreatWar_Defense_v01.unity",
            panel="PANEL_02_CITY_ESTABLISHING__PANEL_03_WORD_MAGIC",
            camPosition=new Vector3(-5.4f,7.8f,25f),
            camTarget=new Vector3(0f,3f,-9f),
            fov=48f,
            ambient=new Color(.16f,.15f,.21f),
            keyColor=new Color(.51f,.57f,1f),
            keyIntensity=4.0f,
            keyPosition=new Vector3(-6,12,2),
            keyRange=60,
            groundCenter=new Vector3(0,-.12f,0),
            groundSize=new Vector3(29,.2f,40)
        },
        new SetSpec
        {
            name="Family Home - Destroyed",
            glb=AssetRoot+"Prologue_Home_04_Ruined_v01.glb",
            scene=SceneRoot+"Prologue_P04_RuinedHome_Defense_v01.unity",
            panel="PANEL_04_GRIEF__VAN_KNEELS_AMONG_HOME_RUINS",
            camPosition=new Vector3(-2.25f,2.22f,4.85f),
            camTarget=new Vector3(.25f,.70f,1.30f),
            fov=42f,
            ambient=new Color(.17f,.17f,.20f),
            keyColor=new Color(.66f,.70f,.83f),
            keyIntensity=2.5f,
            keyPosition=new Vector3(0f,4f,1f),
            keyRange=12,
            groundCenter=new Vector3(0,-.12f,0),
            groundSize=new Vector3(10,.2f,12)
        }
    };

    [MenuItem("SPELLSTRIKE/Cinematics/Defense Prototype/1 Import 3 Blender Environments")]
    private static void CreateThreeScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.sceneCount != 1)
        {
            Debug.LogError("[Defense Sets] One saved clean scene in Edit Mode required.");
            return;
        }
        var initial=SceneManager.GetActiveScene();
        if (!initial.IsValid() || !initial.isLoaded || initial.isDirty ||
            string.IsNullOrWhiteSpace(initial.path))
        {
            Debug.LogError("[Defense Sets] Save current scene before running; no changes made.");
            return;
        }
        foreach (var s in Sets)
        {
            if (File.Exists(s.scene))
            {
                Debug.LogError("[Defense Sets] Refuse overwrite: "+s.scene);
                return;
            }
            if (!File.Exists(s.glb))
            {
                Debug.LogError("[Defense Sets] Blender GLB is missing: "+s.glb);
                return;
            }
        }
        var gltfAssets=new GameObject[Sets.Length];
        for(int i=0;i<Sets.Length;i++)
        {
            AssetDatabase.ImportAsset(Sets[i].glb,ImportAssetOptions.ForceUpdate);
            gltfAssets[i]=AssetDatabase.LoadAssetAtPath<GameObject>(Sets[i].glb);
            if (gltfAssets[i]==null)
            {
                Debug.LogError("[Defense Sets] glTFast import failed: "+Sets[i].glb);
                return;
            }
        }
        string previous=initial.path;
        try
        {
            for(int i=0;i<Sets.Length;i++)
            {
                var spec=Sets[i];
                var created=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var env=(GameObject)PrefabUtility.InstantiatePrefab(gltfAssets[i],created);
                if (env==null)
                    throw new InvalidOperationException("Cannot instantiate "+spec.glb);
                env.name="BLENDER_EXPORT__"+Path.GetFileNameWithoutExtension(spec.glb);
                env.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                env.transform.localScale=Vector3.one;

                var actors=new GameObject("ACTOR_BLOCKING_SOCKETS__ASSIGN_APPROVED_RIGS");
                // Imported anchor empties are already part of the GLB.
                // Do not fake Nevali's missing rig or insert Old Van in its place.
                actors.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                actors.transform.localScale=Vector3.one;
                var camera=CreateCamera(spec.panel,spec.camPosition,spec.camTarget,spec.fov);
                camera.tag="MainCamera";
                var cameraComponent=camera.GetComponent<Camera>();
                cameraComponent.clearFlags=CameraClearFlags.Skybox;
                camera.AddComponent<AudioListener>();

                if (spec.panel.Contains("PANEL_02"))
                {
                    // Separate disabled panel 03 shot; enable one camera at a
                    // time during narration/mocap blocking.
                    var action=CreateCamera("PANEL_03_VAN_REAR_CASTING__DISABLED",
                        new Vector3(-1.5f,2.9f,8.3f),new Vector3(0,5,-5),51f);
                    action.SetActive(false);
                }

                var lighting=new GameObject("PREVIEW_LIGHTING__NO_SHADOWS");
                var warm=new GameObject("KEY_STORY_LIGHT");
                warm.transform.SetParent(lighting.transform,false);
                warm.transform.position=spec.keyPosition;
                var light=warm.AddComponent<Light>();
                light.type=LightType.Point;
                light.color=spec.keyColor;
                light.intensity=spec.keyIntensity;
                light.range=spec.keyRange;
                light.shadows=LightShadows.None;
                RenderSettings.ambientMode=AmbientMode.Flat;
                RenderSettings.ambientLight=spec.ambient;
                RenderSettings.fog=false;

                var collision=new GameObject("CAMERA_ACTOR_BLOCKING__GROUND_ONLY");
                collision.transform.position=spec.groundCenter;
                collision.AddComponent<BoxCollider>().size=spec.groundSize;

                EditorSceneManager.MarkSceneDirty(created);
                if(!EditorSceneManager.SaveScene(created,spec.scene))
                    throw new IOException("Could not save isolated scene "+spec.scene);
                Debug.Log("[Defense Sets] SCENE CREATED: "+spec.name+
                          " from Blender GLB, "+spec.scene);
            }
            AssetDatabase.SaveAssets();
            // Restore the user's previously open cinematic scene.
            EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);
            Debug.Log("[Defense Sets] ALL THREE BLENDER PROTOTYPE SCENES SAVED; "+
                      "original scene restored. Source actors/narration not added. "+
                      "No build routing or gameplay scene changes.");
        }
        catch(Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("[Defense Sets] Check partial scene assets before rerunning; "+
                           "the builder never overwrites existing scenes.");
        }
    }

    private static GameObject CreateCamera(string name,Vector3 position,
                                            Vector3 target,float fov)
    {
        var obj=new GameObject(name);
        obj.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(target-position,Vector3.up));
        var cam=obj.AddComponent<Camera>();
        cam.fieldOfView=fov;
        cam.nearClipPlane=.1f;
        cam.farClipPlane=110f;
        cam.allowHDR=false;
        return obj;
    }
}
#endif
