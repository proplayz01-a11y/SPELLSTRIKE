using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModeSelectTilePreviewQA
{
    [MenuItem("SPELLSTRIKE/Minigames/QA - Inspect V11 Tile Preview RenderTexture")]
    public static void Inspect()
    {
        if (System.Diagnostics.Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0) return;
        GameObject gallery = GameObject.Find("V11_SPEL_3D_AssetReuse");
        GameObject cameraGo = GameObject.Find("V11_SP_E_L_DedicatedPreviewCamera");
        if (gallery == null || cameraGo == null) { Debug.LogError("[V11 QA] Gallery or camera missing"); return; }
        var renderers = gallery.GetComponentsInChildren<Renderer>(true);
        Camera cam = cameraGo.GetComponent<Camera>();
        Debug.Log("[V11 QA] Active scene " + gallery.scene.path + " renderers=" + renderers.Length +
            " cameraMask=" + cam.cullingMask + " texture=" + cam.targetTexture +
            " orthographic=" + cam.orthographic + " cameraEnabled=" + cam.enabled +
            " additionalComponents=" + string.Join(",", Array.ConvertAll(cameraGo.GetComponents<Component>(), c => c.GetType().Name)));
        for (int i = 0; i < Math.Min(15,renderers.Length);i++)
        {
            var ren = renderers[i];
            Debug.Log("[V11 QA] " + ren.name + " layer=" + ren.gameObject.layer +
                " enabled=" + ren.enabled + " active=" + ren.gameObject.activeInHierarchy +
                " bounds=" + ren.bounds + " mat=" + (ren.sharedMaterial == null ? "NONE" : ren.sharedMaterial.shader.name));
        }
        RenderTexture rt = cam.targetTexture;
        if (rt == null) { Debug.LogError("[V11 QA] RenderTexture missing"); return; }
        RenderTexture prior = RenderTexture.active;
        try
        {
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
            t.Apply();
            Color32[] colors = t.GetPixels32();
            int nonzero=0, bright=0; long sumA=0;
            foreach(var px in colors) { if(px.a>5)nonzero++; if(px.r+px.g+px.b>100)bright++; sumA+=px.a; }
            string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads","MAIN MENU MODE SELECT ASSETS","UNITY_INTEGRATION_QA");
            Directory.CreateDirectory(directory);
            string output=Path.Combine(directory,"V11_TilePreview_RenderTexture_Diagnostic.png");
            File.WriteAllBytes(output,t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            Debug.Log("[V11 QA] PREVIEW TEXTURE pixels=" + colors.Length + " alpha>5=" + nonzero +
                " nonDark=" + bright + " avgAlpha=" + ((float)sumA/colors.Length) + " screenshot=" + output);
        }
        finally { RenderTexture.active = prior; }
    }
}
