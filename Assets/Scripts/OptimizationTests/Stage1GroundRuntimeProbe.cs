#if UNITY_EDITOR || SPELLSTRIKE_STAGE1_A05S_TEST
using System.Collections;
using UnityEngine;

internal sealed class Stage1GroundRuntimeProbe : MonoBehaviour
{
    private Transform player;
    private float startY;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        // This probe is strictly for the dedicated optimization scene. It used to
        // install on every Editor Play session and forcibly stop Play Mode after
        // 15 seconds, which breaks normal Main Menu / SampleScene testing.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
            "Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity")
            return;

        var go = new GameObject("__Stage1GroundRuntimeProbe");
        DontDestroyOnLoad(go);
        go.AddComponent<Stage1GroundRuntimeProbe>();
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < 120 && player == null; i++)
        {
            GameObject candidate = GameObject.Find("VAN AERT");
            if (candidate != null)
                player = candidate.transform;
            else
                yield return null;
        }

        if (player == null)
        {
            Debug.LogError("[Stage1 Ground Runtime Probe] FAIL player not found.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
            yield break;
        }

        startY = player.position.y;
        Debug.Log($"[Stage1 Ground Runtime Probe] START y={startY:F3} position={player.position}");

        float elapsed = 0f;
        while (elapsed < 15f && player != null)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            Debug.Log($"[Stage1 Ground Runtime Probe] t={elapsed:F0}s y={player.position.y:F3} grounded={player.GetComponent<CharacterController>()?.isGrounded}");
        }

        if (player == null)
        {
            Debug.LogError("[Stage1 Ground Runtime Probe] FAIL player destroyed during probe.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
            yield break;
        }

        float endY = player.position.y;
        bool pass = endY > -150f && Mathf.Abs(endY - startY) < 30f;
        Debug.Log($"[Stage1 Ground Runtime Probe] {(pass ? "PASS" : "FAIL")} startY={startY:F3} endY={endY:F3} delta={endY - startY:F3}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#endif
    }
}
#endif
