#if UNITY_EDITOR
// Author ONLY the first storyboard motion/camera blocking pass in the isolated
// cinematic StudyArchive scene. Run explicitly from the visible Unity menu.
// Never touches StoryScene, Tutorial, Stage Select, gameplay prefabs or builds.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

internal static class StudyArchivePanels1518TimelineBuilder
{
    private const string ScenePath =
        "Assets/Scenes/Cinematics/StudyArchive_CinematicSet_v01.unity";
    private const string TimelineFolder =
        "Assets/Cinematics/Storyboard3D/Timelines/Panels15_18";
    private const string TimelinePath =
        TimelineFolder + "/Prologue_P15_P18_ArchiveRetrieval_Blocking_v01.playable";
    private const string VanderPath =
        "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Prefabs/VanderPoel_CinematicActor.prefab";
    // Current production Van Aert (updated visual + canonical rig), never the
    // older legacy "VAN AERT.prefab" demo actor.
    private const string VanPath = "Assets/Prefabs/VanAertPlayer.prefab";
    private const string VanderAnims =
        "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Animations/";
    private const string VanAnims = "Assets/VAN AERT/ANIMS/";
    private const string ClipVanderMotion =
        TimelineFolder + "/Block_Vander_Path_v01.anim";
    private const string ClipVanMotion =
        TimelineFolder + "/Block_Van_Path_v01.anim";
    private const string LeatherMaterial =
        TimelineFolder + "/PrototypeBook_Leather_URP.mat";
    private const string PageMaterial =
        TimelineFolder + "/PrototypeBook_Parchment_URP.mat";
    private const string SceneBackup =
        "Library/StoryboardTimeline_P15_P18_20261023/StudyArchive_BEFORE_StoryboardTimeline.unity";

    private const double Duration = 28.0;
    private const string BlockRoot = "STORYBOARD_P15_P18__TIMELINE_BLOCKING_V01";

    [MenuItem("SPELLSTRIKE/Cinematics/Library/9 Build Panels 15-18 Timeline Blocking %#t")]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.sceneCount != 1 || !scene.IsValid() || !scene.isLoaded ||
            scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[P15-18] Open and SAVE only StudyArchive_CinematicSet_v01 " +
                           "in Edit Mode before running. No changes made.");
            return;
        }
        if (File.Exists(SceneBackup) || AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath) != null ||
            GameObject.Find(BlockRoot) != null)
        {
            Debug.LogError("[P15-18] Previous backup, Timeline or scene blocking exists. " +
                           "Refusing duplicate or overwrite.");
            return;
        }
        try
        {
            GameObject vander = AssetDatabase.LoadAssetAtPath<GameObject>(VanderPath);
            GameObject van = AssetDatabase.LoadAssetAtPath<GameObject>(VanPath);
            AnimationClip vanderSitStand = GetAnimation(VanderAnims + "VanderPoel_SitToStand.fbx");
            AnimationClip vanderWalk = GetAnimation(VanderAnims + "VanderPoel_Walking.fbx");
            AnimationClip vanderPoint = GetAnimation(VanderAnims + "VanderPoel_Point.fbx");
            AnimationClip vanderIdle = GetAnimation(
                "Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Models/VanderPoel_StandingIdle_WithSkin.fbx");
            AnimationClip vanIdle = GetAnimation(VanAnims + "WIZARD-MIXAMO@Idle.fbx");
            AnimationClip vanWalk = GetAnimation(VanAnims + "WIZARD-MIXAMO@Walking.fbx");
            if (vander == null || van == null || vanderWalk == null || vanWalk == null ||
                vanderIdle == null || vanIdle == null || vanderSitStand == null ||
                vanderPoint == null)
                throw new InvalidOperationException("A cinematic prefab or required humanoid clip is unavailable.");

            // Earlier proof scene is valuable: make a write-once backup first.
            Directory.CreateDirectory(Path.GetDirectoryName(SceneBackup));
            File.Copy(ScenePath, SceneBackup, false);
            EnsureFolders();
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader unavailable.");
            var leather=CreateMaterial(LeatherMaterial,shader,new Color(.20f,.095f,.049f),.32f);
            var parchment=CreateMaterial(PageMaterial,shader,new Color(.81f,.68f,.45f),.05f);

            var sequenceRoot=new GameObject(BlockRoot);
            var cast=new GameObject("01_ACTORS__KINEMATIC_BLOCKING");
            cast.transform.SetParent(sequenceRoot.transform,false);
            var cameraRoot=new GameObject("02_SHOTS__P15_P16_P17_P18");
            cameraRoot.transform.SetParent(sequenceRoot.transform,false);
            var props=new GameObject("03_BOOK__SHELF_TO_HAND_TO_TABLE");
            props.transform.SetParent(sequenceRoot.transform,false);
            var directorObject=new GameObject("04_TIMELINE_DIRECTOR__PLAY_ON_AWAKE");
            directorObject.transform.SetParent(sequenceRoot.transform,false);

            Actor vanderActor=MakeActor(scene,cast.transform,vander,
                "VANDER_POEL_BLOCKING_RIG",new Vector3(1.65f,0,6.78f));
            Actor vanActor=MakeActor(scene,cast.transform,van,
                "OLDER_VAN_BLOCKING_RIG",new Vector3(-1.65f,0,6.78f));

            // No fake duplicate library: use actual shelf and study positions.
            // Exactly one physical book is represented by activation-switched
            // temporary pose variants. Replace with rigged Living Spellbook
            // after the prototype blocking is approved.
            var shelfBook=CreateBook("BOOK_STATE_01_SHELF_CLOSED__PROTOTYPE",
                props.transform,leather,parchment,
                new Vector3(3.56f,1.70f,-5.66f),Quaternion.Euler(0,90f,0));
            var carriedBook=CreateBook("BOOK_STATE_02_CARRIED_BY_VANDER__PROTOTYPE",
                vanderActor.motionRoot.transform,leather,parchment,
                new Vector3(-.29f,1.18f,.34f),Quaternion.Euler(0,12f,-20f));
            var tableBook=CreateBook("BOOK_STATE_03_ON_STUDY_TABLE__PROTOTYPE",
                props.transform,leather,parchment,
                new Vector3(0,.91f,6.76f),Quaternion.Euler(0,90f,0));

            Camera panel15=CreateCamera(cameraRoot.transform,"P15_TwoWizardsInvitation",
                new Vector3(0,2.55f,10.85f),new Vector3(0,1.45f,6.65f),52f);
            Camera panel16=CreateCamera(cameraRoot.transform,"P16_GatewayTrackingView",
                new Vector3(1.65f,2.18f,6.45f),new Vector3(0,1.4f,-.9f),56f);
            Camera panel17=CreateCamera(cameraRoot.transform,"P17_AncientBookRetrieval",
                new Vector3(1.35f,2.08f,-4.2f),new Vector3(3.1f,1.50f,-5.85f),48f);
            Camera panel18=CreateCamera(cameraRoot.transform,"P18_ReturnToMainStudyTable",
                new Vector3(3.8f,2.22f,9.7f),new Vector3(0,1.11f,6.76f),54f);
            // Our previous preview camera remains available for Edit Mode,
            // but the Timeline owns ALL runtime camera switching.
            var oldMain=Find(scene,"MAIN_CINEMATIC_CAMERA__STUDY_TO_ARCHIVE");
            if (oldMain == null) throw new InvalidOperationException("Existing main preview camera missing.");
            // Enable default shot in editor; Timeline manages it in Play.
            oldMain.gameObject.SetActive(false);
            panel15.gameObject.SetActive(true);
            panel16.gameObject.SetActive(false);
            panel17.gameObject.SetActive(false);
            panel18.gameObject.SetActive(false);
            foreach(var camera in new []{panel15,panel16,panel17,panel18})
            {
                var existingListener = camera.GetComponent<AudioListener>();
                if (existingListener != null) UnityEngine.Object.DestroyImmediate(existingListener);
            }
            directorObject.AddComponent<AudioListener>();
            // One 2D listener survives every camera cut without duplicates.

            TimelineAsset timeline=ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name="Prologue_P15_P18_ArchiveRetrieval_Blocking_v01";
            AssetDatabase.CreateAsset(timeline,TimelinePath);
            var director=directorObject.AddComponent<PlayableDirector>();
            director.playableAsset=timeline;
            director.playOnAwake=true;
            director.extrapolationMode=DirectorWrapMode.Hold;

            var vanderPath=BuildPositionClip(ClipVanderMotion,
                new [] {0f,3f,5f,9f,12.5f,15.5f,17.5f,21.5f,25f,28f},
                new [] {
                    new Vector3(1.65f,0,6.78f),new Vector3(1.75f,0,6.88f),
                    new Vector3(1.53f,0,5.0f),new Vector3(.46f,0,.75f),
                    new Vector3(2.70f,0,-4.95f),new Vector3(2.70f,0,-4.95f),
                    new Vector3(2.70f,0,-4.95f),new Vector3(.55f,0,2.80f),
                    new Vector3(1.35f,0,6.76f),new Vector3(1.35f,0,6.76f)
                });
            var vanPath=BuildPositionClip(ClipVanMotion,
                new [] {0f,3f,5f,9f,12.5f,15.5f,17.5f,21.5f,25f,28f},
                new [] {
                    new Vector3(-1.65f,0,6.78f),new Vector3(-1.65f,0,6.78f),
                    new Vector3(-1.55f,0,5.6f),new Vector3(-.65f,0,1.6f),
                    new Vector3(-.75f,0,-2.7f),new Vector3(-.75f,0,-3.6f),
                    new Vector3(-.75f,0,-3.6f),new Vector3(-.60f,0,2.8f),
                    new Vector3(-1.65f,0,6.76f),new Vector3(-1.65f,0,6.76f)
                });
            BindMotion(timeline,director,vanderActor.motionAnimator,
                "Vander BLOCKED MOTION (study -> shelf -> study)",vanderPath);
            BindMotion(timeline,director,vanActor.motionAnimator,
                "Van BLOCKED MOTION (follow -> return)",vanPath);
            var vanderAnim=timeline.CreateTrack<AnimationTrack>(null,"Vander HUMANOID motions");
            director.SetGenericBinding(vanderAnim,vanderActor.visualAnimator);
            AddAnimation(vanderAnim,vanderSitStand,"P15 rise to lead",0,3.3);
            AddAnimation(vanderAnim,vanderPoint,"P15 gesture toward archive",3.3,1.3);
            AddAnimation(vanderAnim,vanderWalk,"P16 walk into archive",4.6,8.5,true);
            AddAnimation(vanderAnim,vanderIdle,"P17 steady before book",13.1,2.4);
            AddAnimation(vanderAnim,vanderPoint,"P17 reaching pose placeholder",15.5,1.8);
            AddAnimation(vanderAnim,vanderWalk,"P18 return carrying book",17.3,7.5,true);
            AddAnimation(vanderAnim,vanderIdle,"P18 set book / pause",24.8,3.2);

            var vanAnim=timeline.CreateTrack<AnimationTrack>(null,"Older Van HUMANOID motions");
            director.SetGenericBinding(vanAnim,vanActor.visualAnimator);
            AddAnimation(vanAnim,vanIdle,"P15 listen and turn",0,4.6);
            AddAnimation(vanAnim,vanWalk,"P16 follow Vander",4.6,9.0,true);
            AddAnimation(vanAnim,vanIdle,"P17 watch retrieval",13.6,3.7);
            AddAnimation(vanAnim,vanWalk,"P18 follow back to table",17.3,7.7,true);
            AddAnimation(vanAnim,vanIdle,"P18 observe book",25,3);

            AddActivation(timeline,director,panel15.gameObject,"P15 SHOT",0,4.6);
            AddActivation(timeline,director,panel16.gameObject,"P16 SHOT",4.6,8.5);
            AddActivation(timeline,director,panel17.gameObject,"P17 SHOT",13.1,4.2);
            AddActivation(timeline,director,panel18.gameObject,"P18 SHOT",17.3,10.7);
            AddActivation(timeline,director,shelfBook,"Book on shelf",0,15.6);
            AddActivation(timeline,director,carriedBook,"Book in Vander's care",15.6,9.2);
            AddActivation(timeline,director,tableBook,"Book on study table",24.8,3.2);
            // Timeline-native audio tracks reserved for actual recorded voice
            // performances, not synthesized or invented dialogue.
            var voices=timeline.CreateTrack<AudioTrack>(null,"VOICE PERFORMANCES (awaiting recordings)");
            var audio=directorObject.AddComponent<AudioSource>();
            audio.playOnAwake=false; audio.spatialBlend=0;
            director.SetGenericBinding(voices,audio);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new IOException("Could not save the isolated cinematic scene.");

            Debug.Log("[P15-18] TIMELINE BLOCKING BUILD PASS: 28-second Timeline, " +
                      "2 animated actors, 2 kinematic path tracks, humanoid animation " +
                      "tracks, 4 camera cut tracks, 3 book-state tracks, voice track; " +
                      "created in isolated StudyArchive only. Backup: "+SceneBackup);
            Selection.activeGameObject=directorObject;
        }
        catch(Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[P15-18] Prototype build incomplete. Check backup and " +
                           "created assets before any rerun; do NOT overwrite.");
        }
    }

    private struct Actor
    {
        public GameObject motionRoot;
        public Animator motionAnimator;
        public Animator visualAnimator;
    }

    private static Actor MakeActor(Scene scene,Transform parent,GameObject visualPrefab,
                                   string name,Vector3 start)
    {
        var mover=new GameObject(name);
        mover.transform.SetParent(parent,false);
        mover.transform.localPosition=start;
        var motionAnimator=mover.AddComponent<Animator>();
        motionAnimator.applyRootMotion=false;
        motionAnimator.runtimeAnimatorController=null;
        motionAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        GameObject visual=(GameObject)PrefabUtility.InstantiatePrefab(visualPrefab,scene);
        if(visual==null)throw new InvalidOperationException("Could not instantiate actor "+name);
        visual.name=name+"_VISUAL_ONLY";
        visual.transform.SetParent(mover.transform,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        visual.transform.localScale=Vector3.one;

        foreach(var component in visual.GetComponentsInChildren<MonoBehaviour>(true))
            if(component!=null) component.enabled=false;
        foreach(var collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled=false;
        foreach(var rb in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic=true;
            rb.detectCollisions=false;
        }
        foreach(var camera in visual.GetComponentsInChildren<Camera>(true))
            camera.enabled=false;
        foreach(var listener in visual.GetComponentsInChildren<AudioListener>(true))
            listener.enabled=false;

        // The kinematic parent Animator is not the skeletal Humanoid Animator.
        Animator rig=visual.GetComponentsInChildren<Animator>(true)
            .FirstOrDefault(x=>x!=motionAnimator && x.avatar!=null);
        if(rig==null)
            throw new InvalidOperationException("No imported rigged Animator on "+name);
        rig.runtimeAnimatorController=null;
        rig.applyRootMotion=false;
        rig.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        return new Actor {motionRoot=mover,motionAnimator=motionAnimator,visualAnimator=rig};
    }

    private static AnimationClip GetAnimation(string path)
    {
        if(!File.Exists(path))return null;
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c=>!c.name.StartsWith("__preview__",StringComparison.OrdinalIgnoreCase));
    }

    private static void AddAnimation(AnimationTrack track,AnimationClip source,
                                      string label,double start,double duration,
                                      bool repeatWalkingCycle=false)
    {
        if(source==null) throw new InvalidOperationException("Missing animation: "+label);
        TimelineClip clip=track.CreateClip(source);
        clip.displayName=label;
        clip.start=start;
        clip.duration=duration;
        // A short Mixamo walking cycle must repeat for the entire authored
        // travel window. Timeline separately animates actor root transforms.
        if (repeatWalkingCycle && clip.asset is AnimationPlayableAsset walk)
            walk.loop=AnimationPlayableAsset.LoopMode.On;
        clip.blendInDuration=Math.Min(.16,duration*.1);
        clip.blendOutDuration=Math.Min(.16,duration*.1);
    }

    private static AnimationClip BuildPositionClip(string path,float[] times,Vector3[] positions)
    {
        if(times.Length!=positions.Length||times.Length<2)
            throw new ArgumentException("Invalid storyboard path points.");
        var clip=new AnimationClip {name=Path.GetFileNameWithoutExtension(path),
                                    legacy=false,frameRate=30};
        for(int axis=0;axis<3;axis++)
        {
            var keys=new Keyframe[times.Length];
            for(int i=0;i<keys.Length;i++)
                keys[i]=new Keyframe(times[i],positions[i][axis]);
            var curve=new AnimationCurve(keys);
            for(int i=0;i<keys.Length;i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("",typeof(Transform),
                    "m_LocalPosition."+("xyz"[axis])),curve);
        }
        AssetDatabase.CreateAsset(clip,path);
        return clip;
    }

    private static void BindMotion(TimelineAsset timeline,PlayableDirector director,
                                   Animator actor,string name,AnimationClip clip)
    {
        var motion=timeline.CreateTrack<AnimationTrack>(null,name);
        var take=motion.CreateClip(clip);
        take.start=0;
        take.duration=Duration;
        take.displayName=name;
        director.SetGenericBinding(motion,actor);
    }

    private static void AddActivation(TimelineAsset timeline,PlayableDirector director,
                                      GameObject target,string name,double start,double duration)
    {
        var track=timeline.CreateTrack<ActivationTrack>(null,name);
        // Keep the closing study-table shot visible after the Director ends.
        // All-cameras-inactive produced a black Game view in the first smoke test.
        track.postPlaybackState=name=="P18 SHOT"
            ? ActivationTrack.PostPlaybackState.Active
            : ActivationTrack.PostPlaybackState.Inactive;
        var clip=track.CreateDefaultClip();
        clip.displayName=name;
        clip.start=start;
        clip.duration=duration;
        director.SetGenericBinding(track,target);
    }

    private static GameObject CreateBook(string name,Transform parent,
                                         Material leather,Material parchment,
                                         Vector3 position,Quaternion rotation)
    {
        var book=new GameObject(name);
        book.transform.SetParent(parent,false);
        book.transform.localPosition=position;
        book.transform.localRotation=rotation;
        MakePiece(book.transform,"COVER_DarkLeather",new Vector3(0,0,0),
                  new Vector3(.32f,.055f,.42f),leather);
        MakePiece(book.transform,"PAGES_AgedParchment",new Vector3(0,.035f,0),
                  new Vector3(.285f,.05f,.386f),parchment);
        MakePiece(book.transform,"TOP_Cover",new Vector3(0,.068f,0),
                  new Vector3(.32f,.015f,.42f),leather);
        book.SetActive(false);
        return book;
    }

    private static void MakePiece(Transform parent,string name,Vector3 local,Vector3 size,
                                  Material material)
    {
        GameObject box=GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name=name;
        box.transform.SetParent(parent,false);
        box.transform.localPosition=local;
        box.transform.localScale=size;
        box.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(box.GetComponent<BoxCollider>());
    }

    private static Camera CreateCamera(Transform parent,string name,Vector3 at,Vector3 to,float fov)
    {
        GameObject camera=new GameObject(name);
        camera.transform.SetParent(parent,false);
        camera.transform.localPosition=at;
        camera.transform.localRotation=Quaternion.LookRotation(to-at,Vector3.up);
        Camera component=camera.AddComponent<Camera>();
        component.fieldOfView=fov;
        component.nearClipPlane=.1f;
        component.farClipPlane=100;
        return component;
    }

    private static Material CreateMaterial(string path,Shader shader,Color color,float metallic)
    {
        if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null)
            throw new InvalidOperationException("Prototype material exists: "+path);
        var m=new Material(shader) {name=Path.GetFileNameWithoutExtension(path)};
        m.SetColor("_BaseColor",color);
        m.SetFloat("_Metallic",metallic);
        m.SetFloat("_Smoothness",.25f);
        AssetDatabase.CreateAsset(m,path);
        return m;
    }

    private static Transform Find(Scene scene,string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(x=>x.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(x=>x.name==name);
    }

    private static void EnsureFolders()
    {
        string current="Assets/Cinematics/Storyboard3D";
        if(!AssetDatabase.IsValidFolder(current))
            throw new DirectoryNotFoundException(current);
        if(!AssetDatabase.IsValidFolder(current+"/Timelines"))
            AssetDatabase.CreateFolder(current,"Timelines");
        if(!AssetDatabase.IsValidFolder(TimelineFolder))
            AssetDatabase.CreateFolder(current+"/Timelines","Panels15_18");
    }
}
#endif
