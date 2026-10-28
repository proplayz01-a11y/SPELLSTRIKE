#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

internal static class SunboundWardenAnimatorLiveSync
{
    private const string ControllerPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animator/SunboundWardenAnimController.controller";

    private const string RadiantRushClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_RadiantRush_ShortHeavy.anim";
    private const string RadiantRushSprintClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/Sword And Shield Run.anim";
    private const string SunSlashClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_SunSlash.fbx";
    private const string RecoveryClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_Recovery_CrouchIdle.anim";
    private const string SunbindClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/Sword And Shield Casting.anim";
    private const string BeingHitCrouchedClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/BEING HIT CROUCHED.anim";
    private const string CrouchSlashClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/mixamo.com.anim";
    private const string StandingHitClipPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/Sword And Shield Impact.anim";
    private const string StandingHitMaskPath =
        "Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animator/SunboundWarden_UpperBodyHit.mask";
    private const string StandingHitLayerName = "Upper Body Hit";
    private const string StandingHitStateName = "Standing Hit";

    [InitializeOnLoadMethod]
    private static void QueueSync()
    {
        EditorApplication.delayCall += Sync;
    }

    [MenuItem("SPELLSTRIKE/Stage 3/Sync Sunbound Warden Animator")]
    internal static void Sync()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || controller.layers.Length == 0)
            return;

        bool changed = false;

        if (!controller.parameters.Any(parameter => parameter.name == "RadiantRush"))
        {
            controller.AddParameter("RadiantRush", AnimatorControllerParameterType.Trigger);
            changed = true;
        }
        if (!controller.parameters.Any(parameter => parameter.name == "Sunbind"))
        {
            controller.AddParameter("Sunbind", AnimatorControllerParameterType.Trigger);
            changed = true;
        }
        if (!controller.parameters.Any(parameter => parameter.name == "WalkingToPlayer"))
        {
            controller.AddParameter("WalkingToPlayer", AnimatorControllerParameterType.Bool);
            changed = true;
        }

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "IDLE");
        AnimatorState walk = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Great Sword Walk");
        AnimatorState rushStart = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state =>
                state != null &&
                (state.name == "Radiant Rush Start" || state.name == "Radiant Rush"));
        AnimatorState rushSprint = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Radiant Rush Sprint");
        AnimatorState rushSword = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Radiant Rush Sword");
        AnimatorState sunSlash = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Sun Slash");
        AnimatorState recovery = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Recovery_Merged");
        AnimatorState sunbind = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Sunbind");
        AnimatorState beingHitCrouched = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "BEING HIT CROUCHED");
        AnimatorState crouchSlash = machine.states
            .Select(child => child.state)
            .FirstOrDefault(state => state != null && state.name == "Crouch Slash");
        if (beingHitCrouched == null)
        {
            beingHitCrouched = machine.states
                .Select(child => child.state)
                .FirstOrDefault(state =>
                    state != null &&
                    state.name != null &&
                    state.name.ToUpperInvariant() == "BEING HIT CROUCHED");
        }

        if (idle == null)
        {
            Debug.LogError("[SunboundWardenAnimator] IDLE state is missing; Radiant Rush sync aborted.");
            return;
        }

        if (rushStart == null)
        {
            rushStart = machine.AddState("Radiant Rush Start", new Vector3(410f, 210f, 0f));
            changed = true;
        }
        else if (rushStart.name != "Radiant Rush Start")
        {
            rushStart.name = "Radiant Rush Start";
            changed = true;
        }

        if (rushSprint == null)
        {
            rushSprint = machine.AddState("Radiant Rush Sprint", new Vector3(610f, 210f, 0f));
            changed = true;
        }

        if (rushSword == null)
        {
            rushSword = machine.AddState("Radiant Rush Sword", new Vector3(810f, 210f, 0f));
            changed = true;
        }

        if (sunSlash == null)
        {
            sunSlash = machine.AddState("Sun Slash", new Vector3(1010f, 150f, 0f));
            changed = true;
        }

        if (recovery == null)
        {
            recovery = machine.AddState("Recovery_Merged", new Vector3(1010f, 280f, 0f));
            changed = true;
        }

        if (sunbind == null)
        {
            sunbind = machine.AddState("Sunbind", new Vector3(410f, 360f, 0f));
            changed = true;
        }

        if (beingHitCrouched == null)
        {
            beingHitCrouched = machine.AddState("BEING HIT CROUCHED", new Vector3(610f, 360f, 0f));
            changed = true;
        }

        if (crouchSlash == null)
        {
            crouchSlash = machine.AddState("Crouch Slash", new Vector3(810f, 420f, 0f));
            changed = true;
        }

        foreach (AnimatorState staleTransitionState in machine.states
                     .Select(child => child.state)
                     .Where(state =>
                         state != null &&
                         (state.name == "Crouch To Stand" ||
                          state.name == "Stand To Crouch"))
                     .ToArray())
        {
            machine.RemoveState(staleTransitionState);
            changed = true;
        }

        foreach (AnimatorState duplicate in machine.states
                     .Select(child => child.state)
                     .Where(state =>
                         state != null &&
                         state != beingHitCrouched &&
                         state.name != null &&
                         state.name.ToUpperInvariant() == "BEING HIT CROUCHED")
                     .ToArray())
        {
            machine.RemoveState(duplicate);
            changed = true;
        }

        AnimationClip standingHitClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(StandingHitClipPath);
        AvatarMask standingHitMask =
            AssetDatabase.LoadAssetAtPath<AvatarMask>(StandingHitMaskPath);
        if (standingHitMask == null)
        {
            standingHitMask = new AvatarMask
            {
                name = "SunboundWarden_UpperBodyHit"
            };
            AssetDatabase.CreateAsset(standingHitMask, StandingHitMaskPath);
            changed = true;
        }

        if (ConfigureUpperBodyHumanoidMask(standingHitMask))
        {
            EditorUtility.SetDirty(standingHitMask);
            changed = true;
        }

        int standingHitLayerIndex = System.Array.FindIndex(
            controller.layers,
            layer => layer.name == StandingHitLayerName);
        if (standingHitLayerIndex < 0)
        {
            controller.AddLayer(StandingHitLayerName);
            standingHitLayerIndex = System.Array.FindIndex(
                controller.layers,
                layer => layer.name == StandingHitLayerName);
            changed = true;
        }

        AnimatorControllerLayer[] syncedLayers = controller.layers;
        AnimatorControllerLayer standingHitLayer =
            syncedLayers[standingHitLayerIndex];
        if (standingHitLayer.avatarMask != standingHitMask)
        {
            standingHitLayer.avatarMask = standingHitMask;
            changed = true;
        }
        if (standingHitLayer.blendingMode != AnimatorLayerBlendingMode.Override)
        {
            standingHitLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            changed = true;
        }
        if (!Mathf.Approximately(standingHitLayer.defaultWeight, 0f))
        {
            standingHitLayer.defaultWeight = 0f;
            changed = true;
        }
        syncedLayers[standingHitLayerIndex] = standingHitLayer;
        controller.layers = syncedLayers;

        AnimatorStateMachine standingHitMachine = standingHitLayer.stateMachine;
        AnimatorState standingHit = standingHitMachine.states
            .Select(child => child.state)
            .FirstOrDefault(state =>
                state != null && state.name == StandingHitStateName);
        if (standingHit == null)
        {
            standingHit = standingHitMachine.AddState(
                StandingHitStateName,
                new Vector3(300f, 120f, 0f));
            changed = true;
        }

        if (standingHitClip != null && standingHit.motion != standingHitClip)
        {
            standingHit.motion = standingHitClip;
            changed = true;
        }

        if (!Mathf.Approximately(standingHit.speed, 1f))
        {
            standingHit.speed = 1f;
            changed = true;
        }

        if (standingHitMachine.defaultState != standingHit)
        {
            standingHitMachine.defaultState = standingHit;
            changed = true;
        }

        AnimationClip rushClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RadiantRushClipPath);
        if (rushClip != null && rushStart.motion != rushClip)
        {
            rushStart.motion = rushClip;
            changed = true;
        }
        if (rushClip != null && rushSword.motion != rushClip)
        {
            rushSword.motion = rushClip;
            changed = true;
        }

        AnimationClip sprintClip = AssetDatabase
            .LoadAllAssetsAtPath(RadiantRushSprintClipPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip != null && !clip.name.StartsWith("__preview__"));
        if (sprintClip != null && rushSprint.motion != sprintClip)
        {
            rushSprint.motion = sprintClip;
            changed = true;
        }

        AnimationClip sunSlashClip = AssetDatabase
            .LoadAllAssetsAtPath(SunSlashClipPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip != null && !clip.name.StartsWith("__preview__"));
        if (sunSlashClip != null && sunSlash.motion != sunSlashClip)
        {
            sunSlash.motion = sunSlashClip;
            changed = true;
        }

        AnimationClip recoveryClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RecoveryClipPath);
        if (recoveryClip != null && recovery.motion != recoveryClip)
        {
            recovery.motion = recoveryClip;
            changed = true;
        }

        AnimationClip sunbindClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SunbindClipPath);
        if (sunbindClip != null && sunbind.motion != sunbindClip)
        {
            sunbind.motion = sunbindClip;
            changed = true;
        }

        AnimationClip beingHitCrouchedClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(BeingHitCrouchedClipPath);
        if (beingHitCrouchedClip != null && beingHitCrouched.motion != beingHitCrouchedClip)
        {
            beingHitCrouched.motion = beingHitCrouchedClip;
            changed = true;
        }

        if (beingHitCrouchedClip != null)
        {
            AnimationClipSettings hitSettings =
                AnimationUtility.GetAnimationClipSettings(beingHitCrouchedClip);
            if (hitSettings.loopTime)
            {
                hitSettings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(beingHitCrouchedClip, hitSettings);
                EditorUtility.SetDirty(beingHitCrouchedClip);
                changed = true;
            }
        }

        AnimationClip crouchSlashClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(CrouchSlashClipPath);
        if (crouchSlashClip != null && crouchSlash.motion != crouchSlashClip)
        {
            crouchSlash.motion = crouchSlashClip;
            changed = true;
        }
        if (crouchSlashClip != null)
        {
            AnimationClipSettings crouchSlashSettings =
                AnimationUtility.GetAnimationClipSettings(crouchSlashClip);
            if (crouchSlashSettings.loopTime)
            {
                crouchSlashSettings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(crouchSlashClip, crouchSlashSettings);
                EditorUtility.SetDirty(crouchSlashClip);
                changed = true;
            }
        }

        // Being Hit Crouched is controller-gated and may only play while the
        // Warden is actually in CrouchGuard/IDLE. Remove the old Any State
        // trigger route so a generic BeingHit trigger can never snap a standing
        // Warden into this crouched-only reaction.
        foreach (AnimatorStateTransition transition in machine.anyStateTransitions
                     .Where(transition =>
                         transition != null &&
                         transition.destinationState == beingHitCrouched)
                     .ToArray())
        {
            machine.RemoveAnyStateTransition(transition);
            changed = true;
        }

        AnimatorStateTransition idleToRush = idle.transitions.FirstOrDefault(transition =>
            transition != null &&
            transition.destinationState == rushStart &&
            transition.conditions.Any(condition => condition.parameter == "RadiantRush"));

        if (idleToRush == null)
        {
            foreach (AnimatorStateTransition transition in idle.transitions
                         .Where(transition =>
                             transition != null &&
                             transition.conditions.Any(condition =>
                                 condition.parameter == "RadiantRush"))
                         .ToArray())
            {
                idle.RemoveTransition(transition);
            }

            idleToRush = idle.AddTransition(rushStart);
            idleToRush.AddCondition(AnimatorConditionMode.If, 0f, "RadiantRush");
            changed = true;
        }

        if (idleToRush.hasExitTime)
        {
            idleToRush.hasExitTime = false;
            changed = true;
        }

        if (!Mathf.Approximately(idleToRush.duration, 0.06f))
        {
            idleToRush.duration = 0.06f;
            changed = true;
        }

        AnimatorStateTransition idleToSunbind = idle.transitions.FirstOrDefault(transition =>
            transition != null &&
            transition.destinationState == sunbind &&
            transition.conditions.Any(condition => condition.parameter == "Sunbind"));

        if (idleToSunbind == null)
        {
            idleToSunbind = idle.AddTransition(sunbind);
            idleToSunbind.AddCondition(AnimatorConditionMode.If, 0f, "Sunbind");
            changed = true;
        }

        if (idleToSunbind.hasExitTime)
        {
            idleToSunbind.hasExitTime = false;
            changed = true;
        }

        if (!Mathf.Approximately(idleToSunbind.duration, 0.06f))
        {
            idleToSunbind.duration = 0.06f;
            changed = true;
        }

        if (walk != null)
        {
            AnimatorStateTransition idleToWalkPlayer = idle.transitions.FirstOrDefault(transition =>
                transition != null &&
                transition.destinationState == walk &&
                transition.conditions.Any(condition => condition.parameter == "WalkingToPlayer"));

            if (idleToWalkPlayer == null)
            {
                idleToWalkPlayer = idle.AddTransition(walk);
                idleToWalkPlayer.AddCondition(AnimatorConditionMode.If, 0f, "WalkingToPlayer");
                idleToWalkPlayer.hasExitTime = false;
                idleToWalkPlayer.duration = 0.08f;
                changed = true;
            }

            // There must be exactly one walk -> idle exit. Multiple transitions
            // are OR'ed by Unity, which meant the old not-WalkingToTiles exit
            // instantly kicked us out while WalkingToPlayer was true.
            AnimatorStateTransition[] walkToIdle = walk.transitions
                .Where(transition => transition != null && transition.destinationState == idle)
                .ToArray();

            bool combinedExitAlreadyValid =
                walkToIdle.Length == 1 &&
                !walkToIdle[0].hasExitTime &&
                Mathf.Approximately(walkToIdle[0].duration, 0.08f) &&
                walkToIdle[0].conditions.Length == 2 &&
                walkToIdle[0].conditions.Any(condition =>
                    condition.mode == AnimatorConditionMode.IfNot &&
                    condition.parameter == "WalkingToTiles") &&
                walkToIdle[0].conditions.Any(condition =>
                    condition.mode == AnimatorConditionMode.IfNot &&
                    condition.parameter == "WalkingToPlayer");

            if (!combinedExitAlreadyValid)
            {
                foreach (AnimatorStateTransition transition in walkToIdle)
                    walk.RemoveTransition(transition);

                AnimatorStateTransition combinedWalkExit = walk.AddTransition(idle);
                combinedWalkExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "WalkingToTiles");
                combinedWalkExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "WalkingToPlayer");
                combinedWalkExit.hasExitTime = false;
                combinedWalkExit.duration = 0.08f;
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(rushStart);
            EditorUtility.SetDirty(rushSprint);
            EditorUtility.SetDirty(rushSword);
            EditorUtility.SetDirty(sunSlash);
            EditorUtility.SetDirty(recovery);
            EditorUtility.SetDirty(sunbind);
            EditorUtility.SetDirty(beingHitCrouched);
            EditorUtility.SetDirty(crouchSlash);
            EditorUtility.SetDirty(standingHitMachine);
            EditorUtility.SetDirty(standingHit);
            AssetDatabase.SaveAssets();
        }

        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log(
            $"[SunboundWardenAnimator] Live sync complete. Base combat + Crouch Slash + crouched hit + upper-body standing hit layer present. Changed={changed}");
    }

    private static bool ConfigureUpperBodyHumanoidMask(AvatarMask mask)
    {
        bool changed = false;

        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
            bool shouldBeActive =
                part == AvatarMaskBodyPart.Body ||
                part == AvatarMaskBodyPart.Head ||
                part == AvatarMaskBodyPart.LeftArm ||
                part == AvatarMaskBodyPart.RightArm ||
                part == AvatarMaskBodyPart.LeftFingers ||
                part == AvatarMaskBodyPart.RightFingers ||
                part == AvatarMaskBodyPart.LeftHandIK ||
                part == AvatarMaskBodyPart.RightHandIK;

            if (mask.GetHumanoidBodyPartActive(part) == shouldBeActive)
                continue;

            mask.SetHumanoidBodyPartActive(part, shouldBeActive);
            changed = true;
        }

        return changed;
    }
}
#endif
