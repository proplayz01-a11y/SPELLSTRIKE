#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class Panel04A04ImportedBoundsAudit
{
    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v01.unity");
            string[] prefixes =
            {
                "P04_STATIC_YoungVanAert_Rodin_Mesh",
                "P04_STATIC_P04_WIFE_STATIC_FACE_HAIR_BOOTS_v11",
                "P04_STATIC_P04_MOTHER_STATIC_DRESS_GROUNDED_v10",
                "P04_STATIC_P04_Nevali_Bow_CenterKnot",
                "P04_STATIC_NEVALI_MOTHER_RIGHT_HAND_RETOPO_SKINNED_v12"
            };
            foreach (string key in prefixes)
            {
                var all = UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                var obj = all.FirstOrDefault(t => t.name.StartsWith(key, StringComparison.Ordinal));
                if (obj == null) throw new Exception("Missing "+key);
                var r = obj.GetComponent<Renderer>();
                if(r == null) throw new Exception("Renderer absent " + obj.name);
                Bounds b = r.bounds;
                Debug.Log("[P04 BOUNDS] " + key + " center=" + b.center +
                    " min=" + b.min + " max=" + b.max + " size=" + b.size +
                    " object world position=" + obj.position);
            }
            Debug.Log("[P04 BOUNDS] SUCCESS");
        }
        catch(Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
#endif
