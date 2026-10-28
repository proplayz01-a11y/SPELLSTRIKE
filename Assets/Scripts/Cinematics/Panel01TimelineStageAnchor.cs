using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Reapplies the verified scene-stage transform after Timeline evaluates the
/// imported Generic FBX motion. Unity Timeline otherwise interprets the FBX's
/// root offset as a world-space displacement and pulls Nevali off the rug.
/// Only used in the isolated A01 v05 Timeline scene.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(30000)]
public sealed class Panel01TimelineStageAnchor : MonoBehaviour
{
    public PlayableDirector director;
    public Transform character;
    public Vector3 stagePosition = new Vector3(-1.28f, 0.45f, 0.45f);
    public Vector3 stageRotationEuler = Vector3.zero;

    private void LateUpdate()
    {
        if (!Application.isPlaying || director == null || character == null ||
            director.playableAsset == null)
            return;

        RestoreStage();
    }

    /// <summary>Useful for controlled timeline scrubbing/camera screenshot tests.</summary>
    public void RestoreStage()
    {
        if (character == null) return;
        character.SetPositionAndRotation(stagePosition,
            Quaternion.Euler(stageRotationEuler));
    }
}
