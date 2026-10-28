#if SPELLSTRIKE_STAGE1_A05S_TEST
using UnityEngine;

internal static class Stage1A05sTextureBudget
{
    private const int TestMipLimit = 2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyBeforeSceneLoad()
    {
        QualitySettings.globalTextureMipmapLimit = TestMipLimit;
        Debug.Log($"[Stage1 A05s Texture Budget] globalTextureMipmapLimit={QualitySettings.globalTextureMipmapLimit} (2 = quarter linear resolution / ~1/16 top-level texel area)");
    }
}
#endif
