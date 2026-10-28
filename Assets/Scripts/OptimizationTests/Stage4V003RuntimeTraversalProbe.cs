// Only used by the separately created Stage4 v003 runtime QA scene.
// This is an isolated player-SIZED CharacterController collision probe, not
// the canonical Van Aert movement script, boss combat or progression flow.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public sealed class Stage4V003RuntimeTraversalProbe : MonoBehaviour
{
    private const string ExpectedScene = "Stage4Scene_ConvergenceCitadel_v003_RuntimeQA";
    // Original full-pass 6/8 report is immutable. Re-run only the two failed
    // directions to diagnose the block, writing a separately named report.
    private const string ReportName = "UnityRuntimeAllBridges_AfterBridge4Proxy_20261020.md";
    [SerializeField] private NavMeshData playerSizedNavMesh;
    [SerializeField, Min(5f)] private float speed = 65f;
    private NavMeshDataInstance navHandle;
    private GameObject traveler;
    private CharacterController capsule;
    private readonly StringBuilder report = new StringBuilder();

    // World coordinates are actual Unity imported (glTFast right->left conversion).
    private readonly Vector3[,] legs = {
        {new Vector3(-50.663858f, 0f, -24.230541f), new Vector3(-186.363934f, 6f, -89.13058f)},
        {new Vector3(-364.870285f, 6f, -176.435143f), new Vector3(-527.401886f, 12f, -257.70094f)},
        {new Vector3(-721.415405f, 12f, -352.62872f), new Vector3(-1007.236816f, 18f, -489.43959f)},
        {new Vector3(-1551.92871f, 18f, -752.28964f), new Vector3(-1864.61206f, 30f, -904.40588f)}
    };

    public void Configure(NavMeshData navMesh) => playerSizedNavMesh = navMesh;

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != ExpectedScene || playerSizedNavMesh == null)
        {
            Debug.LogError("[Stage4 RuntimeQA] WRONG SCENE OR NAVMESH; NO COLLISION TEST RUN.");
            enabled = false;
            yield break;
        }

        report.AppendLine("# Stage4 v003 real Play Mode capsule collision traversal (NOT canonical player)");
        report.AppendLine("Dedicated Stage4 RuntimeQA scene only, original VanAert/Titan prefabs inactive. No GameDatabaseManager, objectives, unlocks or PlayerPrefs writes by this test.");
        report.AppendLine("Real CharacterController.Move + imported MeshCollider contact while traversing all four bridge decks forwards and backwards. This is not player input/animation or Titan combat.");
        report.AppendLine($"- Capsule: height 21.42, radius 5.355, step 3.213, slope 45; speed={speed:F1} units/s");

        navHandle = NavMesh.AddNavMeshData(playerSizedNavMesh);
        if (!navHandle.valid)
        {
            report.AppendLine("- FAIL: Could not register independent QA NavMesh");
            SaveReport();
            yield break;
        }

        traveler = new GameObject("QA_MobileSized_CharacterController_NOT_VanAert");
        traveler.transform.SetParent(transform, false);
        capsule = traveler.AddComponent<CharacterController>();
        capsule.height = 21.42f;
        capsule.radius = 5.355f;
        capsule.center = new Vector3(0f, 10.71f, 0f);
        capsule.stepOffset = 3.213f;
        capsule.slopeLimit = 45f;
        capsule.skinWidth = 0.45f;
        capsule.minMoveDistance = 0f;
        traveler.transform.position = new Vector3(6f, 3f, 12f);
        yield return null;

        report.AppendLine("## Bridge 4 authoritative Unity MeshCollider raycast height profile");
        float previousY = float.NaN;
        float previousT = 0f;
        for (int profile = 0; profile <= 60; profile++)
        {
            float t = profile / 60f;
            Vector3 point = Vector3.Lerp(legs[3, 0], legs[3, 1], t);
            var hits = Physics.RaycastAll(point + Vector3.up * 120f, Vector3.down, 250f, ~0, QueryTriggerInteraction.Ignore);
            var deck = hits.Where(h => h.collider != null && (h.collider.name == "COL_Bridge_04_Deck_PHYSICS" || h.collider.name == "QA_Bridge04_Smoothed_WalkSurface"))
                .OrderByDescending(h => h.point.y).FirstOrDefault();
            if (deck.collider == null)
            {
                report.AppendLine($"- Profile t={t:F3} x={point.x:F2} z={point.z:F2}: DECK MISSING");
                previousY = float.NaN;
                continue;
            }
            float dy = float.IsNaN(previousY) ? 0 : deck.point.y - previousY;
            float meters = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(Vector3.Lerp(legs[3, 0], legs[3, 1], previousT).x, Vector3.Lerp(legs[3, 0], legs[3, 1], previousT).z));
            float grade = meters > 0 ? Mathf.Abs(dy) / meters : 0;
            report.AppendLine($"- Profile t={t:F3} x={point.x:F2} z={point.z:F2}: colliderY={deck.point.y:F2} linearY={point.y:F2} deltaY={dy:F2} grade={grade:F2}");
            previousY = deck.point.y;
            previousT = t;
        }

        int passed = 0;
        float arrivalTolerance = capsule.radius * 0.80f; // 4.284 world units, less than one player radius.
        report.AppendLine($"- Endpoint arrival tolerance: {arrivalTolerance:F3} world units (80% of actual capsule radius)");
        for (int i = 0; i < legs.GetLength(0); i++)
        {
            for (int d = 0; d < 2; d++)
            {
                Vector3 from = legs[i, d == 0 ? 0 : 1];
                Vector3 to = legs[i, d == 0 ? 1 : 0];
                if (!NavMesh.SamplePosition(from + Vector3.up * 3f, out var navFrom, 40f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(to + Vector3.up * 3f, out var navTo, 40f, NavMesh.AllAreas))
                {
                    report.AppendLine($"- Bridge {i + 1} direction {d}: FAIL NavMesh endpoint sample missing");
                    continue;
                }
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(navFrom.position, navTo.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    report.AppendLine($"- Bridge {i + 1} direction {d}: FAIL NavMesh path not complete");
                    continue;
                }
                capsule.enabled = false;
                traveler.transform.position = navFrom.position + Vector3.up * 0.75f;
                capsule.enabled = true;
                yield return new WaitForFixedUpdate();
                float time = 0f;
                int deckContacts = 0;
                int unsupported = 0;
                int groundedFrames = 0;
                int totalFrames = 0;
                float worstVerticalDrop = 0f;
                bool arrived = true;
                CollisionFlags lastFlags = CollisionFlags.None;
                int collisionSideFrames = 0;
                for (int c = 1; c < path.corners.Length; c++)
                {
                    Vector3 target = path.corners[c];
                    int iterations = 0;
                    while (Vector2.Distance(new Vector2(traveler.transform.position.x, traveler.transform.position.z), new Vector2(target.x, target.z)) > arrivalTolerance)
                    {
                        if (++iterations > 700 || time > 12f) { arrived = false; break; }
                        float dt = Mathf.Min(Time.deltaTime, 0.05f);
                        Vector3 planar = new Vector3(target.x - traveler.transform.position.x, 0f, target.z - traveler.transform.position.z);
                        Vector3 step = planar.normalized * Mathf.Min(planar.magnitude, speed * dt);
                        float oldY = traveler.transform.position.y;
                        lastFlags = capsule.Move(step + Vector3.down * (14f * dt));
                        if ((lastFlags & CollisionFlags.Sides) != 0) collisionSideFrames++;
                        worstVerticalDrop = Mathf.Max(worstVerticalDrop, Mathf.Max(0f, oldY - traveler.transform.position.y));
                        if (capsule.isGrounded) groundedFrames++;
                        totalFrames++;
                        // Only QA isolated floor/bridge colliders count, not other scenes.
                        var hits = Physics.RaycastAll(new Ray(traveler.transform.position + Vector3.up * 9f, Vector3.down), 25f, ~0, QueryTriggerInteraction.Ignore);
                        var near = hits.Where(h => h.collider != null && (h.collider.name.EndsWith("_PHYSICS", StringComparison.Ordinal) || h.collider.name == "QA_Bridge04_Smoothed_WalkSurface"))
                            .OrderBy(h => Mathf.Abs(h.point.y - traveler.transform.position.y)).FirstOrDefault();
                        if (near.collider == null || Mathf.Abs(near.point.y - traveler.transform.position.y) > 5.0f) unsupported++;
                        else if (near.collider.name.Contains($"COL_Bridge_0{i + 1}_Deck") || near.collider.name == "QA_Bridge04_Smoothed_WalkSurface") deckContacts++;
                        time += dt;
                        yield return null;
                    }
                    if (!arrived) break;
                }
                bool pass = arrived && unsupported < 4 && deckContacts >= 4 && worstVerticalDrop < 6f;
                if (pass) passed++;
                report.AppendLine($"- Bridge {i + 1} {(d == 0 ? "forward" : "reverse")}: {(pass ? "PASS" : "FAIL")}, path={path.status}, corners={path.corners.Length}, actualDeckContacts={deckContacts}, unsupportedFrames={unsupported}, groundedFrames={groundedFrames}/{totalFrames}, maxSingleFrameDrop={worstVerticalDrop:F3}, elapsedSimulation={time:F2}s");
                Vector3 finalPos = traveler.transform.position;
                var planarTarget = new Vector3(navTo.position.x - finalPos.x, 0f, navTo.position.z - finalPos.z);
                Vector3 bottom = finalPos + Vector3.up * capsule.radius;
                Vector3 top = finalPos + Vector3.up * (capsule.height - capsule.radius);
                var obstructions = Physics.CapsuleCastAll(bottom, top, capsule.radius, planarTarget.normalized, 15f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h.collider != null && h.collider.name.EndsWith("_PHYSICS", StringComparison.Ordinal))
                    .OrderBy(h => h.distance).Take(12).ToArray();
                report.AppendLine($"  - start={navFrom.position}; endpoint={navTo.position}; FINAL={finalPos}; remainingXZ={planarTarget.magnitude:F3}; sideCollisionFrames={collisionSideFrames}/{totalFrames}; flagsLast={lastFlags}; navCorners={string.Join("; ", path.corners.Select(p => p.ToString()))}");
                report.AppendLine("  - CapsuleCast obstacle candidates (nearest 15m): " + string.Join(", ", obstructions.Select(h => h.collider.name + "@" + h.distance.ToString("F2"))));
            }
        }
        report.AppendLine($"- Final four-bridge runtime collision regression: {passed}/8 directional bridge runs pass. Original 6/8 pre-proxy failure and Bridge04 2/2 proxy test are retained unchanged. This does not validate the canonical VanAert controls, Titan combat, camera or physical A05s performance.");
        SaveReport();
        Debug.Log("[Stage4 RuntimeQA] Finished: " + passed + "/8. Results in " + ReportName);
    }

    private void SaveReport()
    {
#if UNITY_EDITOR
        var path = Path.Combine(Application.dataPath, "Stage 4/ConvergenceCitadel_v003/Validation", ReportName);
        if (File.Exists(path))
        {
            Debug.LogError("[Stage4 RuntimeQA] Refusing to overwrite existing report " + path);
            return;
        }
        File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
#endif
    }

    private void OnDestroy()
    {
        if (navHandle.valid) navHandle.Remove();
    }
}
