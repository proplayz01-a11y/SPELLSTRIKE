#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public static class TempBrgEnumProbe
{
    [InitializeOnLoadMethod]
    private static void LogValues()
    {
        Debug.Log($"[BRG Enum Probe] KeepAll={(int)BatchRendererGroupStrippingMode.KeepAll}; KeepIfEntitiesGraphics={(int)BatchRendererGroupStrippingMode.KeepIfEntitiesGraphics}; StripAll={(int)BatchRendererGroupStrippingMode.StripAll}");
    }
}
#endif