#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// Safe one-time change to the existing P15-P18 Timeline. Allows 1-2 second
/// Mixamo walking cycles to repeat across 7-9 second walk windows, without
/// modifying source FBX animation clips, actor prefabs, or game scenes.
/// </summary>
internal static class StudyArchiveP1518WalkingLoops
{
    private const string PathToTimeline =
        "Assets/Cinematics/Storyboard3D/Timelines/Panels15_18/" +
        "Prologue_P15_P18_ArchiveRetrieval_Blocking_v01.playable";
    private const string Backup =
        "Library/StoryboardTimeline_P15_P18_20261023/" +
        "Prologue_P15_P18_ArchiveRetrieval_BEFORE_WalkLoop_v01.playable";

    private static readonly Dictionary<string,string[]> WalkingClips =
        new Dictionary<string,string[]>
        {
            { "Vander HUMANOID motions",
                new [] { "P16 walk into archive", "P18 return carrying book" } },
            { "Older Van HUMANOID motions",
                new [] { "P16 follow Vander", "P18 follow back to table" } }
        };

    [MenuItem("SPELLSTRIKE/Cinematics/Library/11 Loop Existing P15-18 Walk Cycles")]
    private static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[P15-18 WalkLoop] Edit Mode only. Nothing changed.");
            return;
        }
        if (File.Exists(Backup))
        {
            Debug.LogError("[P15-18 WalkLoop] Backup already exists. No repeated edits.");
            return;
        }
        var timeline=AssetDatabase.LoadAssetAtPath<TimelineAsset>(PathToTimeline);
        if (timeline==null)
        {
            Debug.LogError("[P15-18 WalkLoop] Existing Timeline missing.");
            return;
        }
        var all=new List<AnimationPlayableAsset>();
        var summaries=new List<string>();
        var lastShot=timeline.GetOutputTracks().OfType<ActivationTrack>()
            .SingleOrDefault(x=>x.name=="P18 SHOT");
        if(lastShot==null || lastShot.postPlaybackState!=ActivationTrack.PostPlaybackState.Inactive)
        {
            Debug.LogError("[P15-18 WalkLoop] Unexpected closing-shot state; refusing edit.");
            return;
        }
        foreach (var pair in WalkingClips)
        {
            var track=timeline.GetOutputTracks().OfType<AnimationTrack>()
                .SingleOrDefault(t=>t.name==pair.Key);
            if(track==null)
            {
                Debug.LogError("[P15-18 WalkLoop] Track missing: "+pair.Key);
                return;
            }
            foreach(var label in pair.Value)
            {
                var clip=track.GetClips().SingleOrDefault(x=>x.displayName==label);
                if(clip==null || !(clip.asset is AnimationPlayableAsset playable) ||
                   playable.clip==null || clip.duration < playable.clip.length)
                {
                    Debug.LogError("[P15-18 WalkLoop] Invalid/short walk window: "+label);
                    return;
                }
                all.Add(playable);
                summaries.Add(label+" ["+clip.duration.ToString("0.0")+"s, " +
                              playable.clip.length.ToString("0.00")+"s source]");
            }
        }
        if(all.Count!=4 || all.Any(x=>x.loop==AnimationPlayableAsset.LoopMode.On))
        {
            Debug.LogError("[P15-18 WalkLoop] Expected four untouched walk clips. " +
                           "Refusing partial repeat.");
            return;
        }
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Backup));
        File.Copy(PathToTimeline,Backup,false);
        foreach(var playable in all)
        {
            playable.loop=AnimationPlayableAsset.LoopMode.On;
            EditorUtility.SetDirty(playable);
        }
        // Required for end-of-sequence review: do not leave the Game view
        // black by turning off the final camera when PlayableDirector ends.
        lastShot.postPlaybackState=ActivationTrack.PostPlaybackState.Active;
        EditorUtility.SetDirty(lastShot);
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        Debug.Log("[P15-18 WalkLoop] PASS: 4 walking cycles now LOOP across " +
            "full travel windows, separate root motion unchanged. " +
            string.Join("; ",summaries)+
            "; final Panel 18 camera stays active after Timeline ends. Backup: "+Backup);
    }
}
#endif
