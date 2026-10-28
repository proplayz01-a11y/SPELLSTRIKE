#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;

internal static class Panel04A04CameraOrientationAudit
{
    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(
                "Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v04.unity");
            var director=UnityEngine.Object.FindFirstObjectByType<PlayableDirector>();
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            if(director==null||camera==null) throw new Exception("Camera/Timeline missing");
            var eyes=new [] {
                new Vector3(.90f,1.70f,2.75f),
                new Vector3(-1.16f,1.79f,-1.42f),
                new Vector3(.85f,1.12f,-.86f),
                new Vector3(-.13f,1.56f,1.78f)
            };
            var look=new [] {
                new Vector3(.75f,.55f,.27f),
                new Vector3(.88f,.55f,.28f),
                new Vector3(.96f,.43f,.25f),
                new Vector3(.13f,.20f,1.02f)
            };
            double[] times={.65,3.20,6.55,8.95};
            for(int i=0;i<times.Length;i++)
            {
                director.time=times[i]; director.Evaluate();
                Quaternion expected=Quaternion.LookRotation((look[i]-eyes[i]).normalized,Vector3.up);
                float degrees=Quaternion.Angle(expected,camera.transform.rotation);
                Debug.Log("[P04 ORIENTATION] shot="+(i+1)+" time="+times[i]
                    +" actualPos="+camera.transform.position+" expectedPos="+eyes[i]
                    +" actualRotation="+camera.transform.rotation.eulerAngles
                    +" expectedRotation="+expected.eulerAngles+" angular_error_deg="+degrees);
            }
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
#endif
