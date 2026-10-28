using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Isolated Wizard's Vault preview scene only. The existing ItemSelectionSceneController
/// uses a legacy 1920x1080 reference in Start(); this restores the 2400x1080 art
/// coordinate system without changing the real controller or production scene.
/// </summary>
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(CanvasScaler))]
public sealed class WizardsVaultPreviewCanvasGuard : MonoBehaviour
{
    private CanvasScaler scaler;

    private void Awake() => scaler = GetComponent<CanvasScaler>();

    private void LateUpdate()
    {
        if (scaler == null) return;
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        if (scaler.referenceResolution != new Vector2(2400f, 1080f))
            scaler.referenceResolution = new Vector2(2400f, 1080f);
        if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }
}
