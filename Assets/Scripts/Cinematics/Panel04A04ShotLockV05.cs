using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Keeps the four static storyboard shots' look direction exact after the
/// Timeline AnimationTrack evaluates. Unity's quaternion curve continuity was
/// smoothing the original hard cuts despite step tangents.
/// This component is exclusive to Panel 04. Actor animation is not implied.
/// </summary>
[DefaultExecutionOrder(30000)]
public sealed class Panel04A04ShotLockV05 : MonoBehaviour
{
    public PlayableDirector director;
    public Vector3[] eyes;
    public Vector3[] targets;
    public float[] cutTimes;

    public int CurrentShotIndex
    {
        get
        {
            if (director == null || cutTimes == null || cutTimes.Length != 4)
                return -1;
            double t = director.time;
            if (t >= cutTimes[3]) return 3;
            if (t >= cutTimes[2]) return 2;
            if (t >= cutTimes[1]) return 1;
            return 0;
        }
    }

    public bool ApplyNow()
    {
        int index = CurrentShotIndex;
        if (index < 0 || eyes == null || targets == null ||
            eyes.Length != 4 || targets.Length != 4)
            return false;
        Vector3 direction = targets[index] - eyes[index];
        if (direction.sqrMagnitude < 0.001f)
            return false;
        transform.SetPositionAndRotation(
            eyes[index],
            Quaternion.LookRotation(direction.normalized, Vector3.up));
        return true;
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
            ApplyNow();
    }
}
