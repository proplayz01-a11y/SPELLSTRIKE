using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Supplies the project's existing desktop/right-side mobile camera gesture
/// to the persistent Cinemachine FreeLook rig.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpellstrikeCinemachineInputBridge : MonoBehaviour, AxisState.IInputAxisProvider
{
    private CombatCamera source;

    public void SetSource(CombatCamera cameraSource)
    {
        source = cameraSource;
    }

    public float GetAxisValue(int axis)
    {
        if (source == null)
            source = FindFirstObjectByType<CombatCamera>();

        return source != null ? source.GetCinemachineAxisValue(axis) : 0f;
    }
}
