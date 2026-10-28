using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SunboundWardenWeaponGrip : MonoBehaviour
{
    [SerializeField] private Transform rightHand;
    private readonly List<FingerPose> fingers = new List<FingerPose>();

    [Serializable]
    private struct FingerPose
    {
        public Transform bone;
        public Quaternion gripLocalRotation;
    }

    public Transform RightHand => rightHand;

    public void Configure(Transform hand)
    {
        rightHand = hand;
        RebuildCache();
    }

    private void Awake()
    {
        RebuildCache();
    }

    private void OnEnable()
    {
        if (fingers.Count == 0)
            RebuildCache();
    }

    private void LateUpdate()
    {
        for (int i = 0; i < fingers.Count; i++)
        {
            FingerPose pose = fingers[i];
            if (pose.bone != null)
                pose.bone.localRotation = pose.gripLocalRotation;
        }
    }

    private void RebuildCache()
    {
        fingers.Clear();
        if (rightHand == null)
            return;

        Transform[] transforms = rightHand.GetComponentsInChildren<Transform>(true);
        foreach (Transform bone in transforms)
        {
            if (bone == rightHand)
                continue;

            string lower = bone.name.ToLowerInvariant();
            if (!IsFingerName(lower))
                continue;

            fingers.Add(new FingerPose
            {
                bone = bone,
                // SetupSunboundWarden applies the authored grip pose in edit mode.
                // Cache that exact pose and re-assert it after Animator evaluation.
                gripLocalRotation = bone.localRotation
            });
        }
    }

    private static bool IsFingerName(string lowerName)
    {
        return lowerName.Contains("thumb") ||
               lowerName.Contains("index") ||
               lowerName.Contains("middle") ||
               lowerName.Contains("ring") ||
               lowerName.Contains("pinky") ||
               lowerName.Contains("little");
    }

}
