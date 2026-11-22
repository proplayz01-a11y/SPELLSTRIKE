#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

internal static class PrologueV07October29FailureAudit
{
    const string V07="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity";
    const string Tutorial="Assets/Scenes/TutorialScene.unity";
    const string V06="Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity";
    const string SHA="283573BAA9EB7C9E5D3CEF9290FFDDC3DF1F026B8C8D65EAF6D84A73CB26EBF4";
    const string Log="Library/PrologueV07_Oct29FailureAudit.txt";
    [MenuItem("SPELLSTRIKE/Cinematics/Prologue V07/18 Audit Oct29 five user failures")]
    static void Audit()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||
           EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.sceneCount!=1||
           SceneManager.GetActiveScene().isDirty||
           System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length!=0||
           Hash(V06)!=SHA){Debug.LogError("[OCT29 AUDIT] unsafe editor/v06.");return;}
        var original=SceneManager.GetActiveScene();
        Scene scene=default, tutorial=default;
        var sb=new StringBuilder();
        try
        {
            scene=EditorSceneManager.OpenScene(V07,OpenSceneMode.Additive);
            var group=scene.GetRootGameObjects().Single(g=>g.name=="MASTER_SHOT_GROUPS__NUMBERED_IN_STORY_ORDER").transform;
            foreach(int p in new[]{5,6,7,8,9,10,11,12,13,14,15,19,20,21,22,25,26,29,30,34,35})
            {
                string n=p==15?"P15_P18_SHOT_ROOT":"P"+p.ToString("00")+"_SHOT_ROOT";
                var shot=group.Find(n);
                if(!shot){sb.AppendLine("MISSING SHOT "+n);continue;}
                sb.AppendLine("\n===== "+n+" =====");
                var director=shot.GetComponentsInChildren<PlayableDirector>(true).FirstOrDefault(d=>d.playableAsset is TimelineAsset);
                if(director)
                {
                    sb.AppendLine("DIRECTOR "+Path(director.transform)+" asset="+AssetDatabase.GetAssetPath(director.playableAsset)+" duration="+director.duration);
                    foreach(var track in ((TimelineAsset)director.playableAsset).GetOutputTracks().OfType<AnimationTrack>())
                    {
                        var clips=track.GetClips().ToArray();
                        foreach(var c in clips)
                        {
                            var asset=c.asset as AnimationPlayableAsset;
                            sb.AppendLine(" WALK_TRACK "+track.name+" target="+(director.GetGenericBinding(track)?director.GetGenericBinding(track).name:"NULL")+
                                " clip="+c.displayName+" start="+c.start+" duration="+c.duration+
                                " motion="+(asset&&asset.clip?AssetDatabase.GetAssetPath(asset.clip):"NULL"));
                        }
                    }
                }
                foreach(var c in shot.GetComponentsInChildren<Camera>(true))
                    sb.AppendLine("CAM "+Path(c.transform)+" world="+c.transform.position+" forward="+c.transform.forward+" enabled="+c.enabled);
                foreach(var t in shot.GetComponentsInChildren<Transform>(true))
                {
                    string lower=t.name.ToLowerInvariant();
                    if(lower.Contains("chair")||lower.Contains("seated")||lower.Contains("sit")||
                       lower.Contains("van")||lower.Contains("book")||lower.Contains("volume")||
                       lower.Contains("shelf")||lower.Contains("staff"))
                    {
                        var b=t.GetComponent<Renderer>();
                        sb.AppendLine("OBJECT "+Path(t)+" active="+t.gameObject.activeSelf+" pos="+t.position+
                            " rot="+t.eulerAngles+" scale="+t.lossyScale+
                            " renderer="+(b?(b.enabled?"ON":"OFF"):"none")+
                            (b?" bounds="+b.bounds:""));
                    }
                }
            }
            tutorial=EditorSceneManager.OpenScene(Tutorial,OpenSceneMode.Additive);
            sb.AppendLine("\n===== TUTORIAL SPAWN AUDIT =====");
            foreach(var root in tutorial.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name.IndexOf("spawn",StringComparison.OrdinalIgnoreCase)<0&&
                       t.name.IndexOf("shrine",StringComparison.OrdinalIgnoreCase)<0&&
                       t.name.IndexOf("bridge",StringComparison.OrdinalIgnoreCase)<0) continue;
                    var marker=t.GetComponent<PlayerSpawnPoint>();
                    sb.AppendLine("MARKER "+Path(t)+" world="+t.position+" local="+t.localPosition+
                        " parent="+(t.parent?t.parent.name:"ROOT")+" pos="+t.position+
                        " PLAYER_SPAWN="+(marker?marker.SpawnId+" scale="+marker.PlayerScale:"NO"));
                }
            Directory.CreateDirectory("Library");
            File.WriteAllText(Log,sb.ToString(),new UTF8Encoding(false));
            Debug.Log("[OCT29 AUDIT] completed "+Log);
        }
        catch(Exception ex){Debug.LogException(ex);}
        finally
        {
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            if(tutorial.IsValid()&&tutorial.isLoaded)EditorSceneManager.CloseScene(tutorial,true);
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
    }
    static string Path(Transform t){var s=t.name;for(var p=t.parent;p;p=p.parent)s=p.name+"/"+s;return s;}
    static string Hash(string p){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","");}
}
#endif
