using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2RuntimeRigProbe
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || scene.name != "Stage2Scene")
            return;

        GameObject probe = new GameObject("Stage2RuntimeRigProbe");
        SceneManager.MoveGameObjectToScene(probe, scene);
        probe.AddComponent<Stage2RuntimeRigProbeBehaviour>();
    }
}

internal sealed class Stage2RuntimeRigProbeBehaviour : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3f);

        Debug.Log($"[Stage2RigAndroid] device={SystemInfo.deviceModel} api={SystemInfo.graphicsDeviceType} gpu={SystemInfo.graphicsDeviceName} skinWeights={QualitySettings.skinWeights}");
        LogCreature<BarnacleHuskController>("Barnacle");
        LogCreature<CorsairPhantomController>("Corsair");
        LogCreature<TideWraithController>("Tide");
        LogCreature<DrownedCaptainController>("Captain");

        Destroy(gameObject);
    }

    private static void LogCreature<T>(string label) where T : MonoBehaviour
    {
        T controller = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.Log($"[Stage2RigAndroid] {label}: controller=NULL");
            return;
        }

        Animator animator = controller.GetComponent<Animator>() ?? controller.GetComponentInChildren<Animator>(true);
        SkinnedMeshRenderer[] renderers = controller.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Transform hips = null;

        if (animator != null)
        {
            if (animator.isHuman)
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (hips == null)
            {
                Transform[] transforms = animator.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in transforms)
                {
                    if (t.name.ToLowerInvariant().Contains("hips"))
                    {
                        hips = t;
                        break;
                    }
                }
            }
        }

        string anim = animator == null
            ? "animator=NULL"
            : $"animObj={animator.gameObject.name} local={animator.transform.localEulerAngles:F2} world={animator.transform.eulerAngles:F2} rootRot={animator.rootRotation.eulerAngles:F2} human={animator.isHuman} avatarValid={(animator.avatar != null && animator.avatar.isValid)}";
        string hip = hips == null
            ? "hips=NULL"
            : $"hips={hips.name} hipsLocal={hips.localEulerAngles:F2} hipsWorld={hips.eulerAngles:F2}";

        Debug.Log($"[Stage2RigAndroid] {label}: root={controller.gameObject.name} local={controller.transform.localEulerAngles:F2} world={controller.transform.eulerAngles:F2} | {anim} | {hip} | renderers={renderers.Length}");

        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer smr = renderers[i];
            Debug.Log($"[Stage2RigAndroid] {label}/SMR{i}: obj={smr.gameObject.name} quality={smr.quality} bones={smr.bones.Length} rootBone={(smr.rootBone != null ? smr.rootBone.name : "NULL")} rootBoneLocal={(smr.rootBone != null ? smr.rootBone.localEulerAngles.ToString("F2") : "NA")} rootBoneWorld={(smr.rootBone != null ? smr.rootBone.eulerAngles.ToString("F2") : "NA")}");
        }
    }
}
