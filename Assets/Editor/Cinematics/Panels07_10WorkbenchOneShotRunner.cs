#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class Panels07_10WorkbenchOneShotRunner
{
    const string Key="SPELLSTRIKE_P07_10_WORKBENCH_ONESHOT_20261027";
    static Panels07_10WorkbenchOneShotRunner()
    {
        EditorApplication.delayCall += Run;
    }
    static void Run()
    {
        if (SessionState.GetBool(Key,false)) return;
        var s=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || !s.IsValid() || !s.isLoaded || s.isDirty || !s.path.EndsWith("Storyboard3D_CinematicWorkbench.unity",StringComparison.Ordinal))
        {Debug.LogWarning("[P07-10] One-shot not run: requires CLEAN saved cinematic workbench, found "+s.path+" dirty="+s.isDirty);return;}
        SessionState.SetBool(Key,true);
        try { Panels07_10TimelineAssemblerCandidateV03.BuildAll();Debug.Log("[P07-10] ONE-SHOT SUCCESS. Unity playback still requires verification."); }
        catch(Exception e){Debug.LogError("[P07-10] ONE-SHOT FAILED: "+e);}
    }
}
#endif

