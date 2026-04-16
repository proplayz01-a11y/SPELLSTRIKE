using System.Collections;
using UnityEngine;

public class SnareRoot : MonoBehaviour
{
    public float riseSpeed = 2f;
    public float strikeSpeed = 12f;
    public float riseHeight = 2f;

    private Vector3 startPos;
    private Transform playerTransform;

    public void Init(Vector3 playerPos)
    {
        startPos = transform.position;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        StartCoroutine(RiseThenStrike());
    }

    IEnumerator RiseThenStrike()
    {
        // 🔼 RISE
        Vector3 raised = startPos + Vector3.up * riseHeight;

        while (Vector3.Distance(transform.position, raised) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, raised, riseSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // ⚔️ STRIKE — track player live
        while (playerTransform != null)
        {
            Vector3 currentTarget = playerTransform.position;
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, strikeSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, currentTarget) <= 0.2f)
                break;

            yield return null;
        }

        Destroy(gameObject);
    }
}