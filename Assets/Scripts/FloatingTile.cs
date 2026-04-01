using UnityEngine;

public class FloatingTile : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatAmplitude = 0.25f; // How high the tile floats
    public float floatFrequency = 1f; // How fast the tile floats
    public Vector3 rotationSpeed = new Vector3(0f, 50f, 0f); // Rotation speed in degrees per second

    [Header("Spawn Animation")]
    public float spawnRiseDistance = 1f; // How high the tile rises when spawned
    public float spawnDuration = 0.5f; // Duration of the spawn animation
    public AnimationCurve spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Curve for spawn animation

    private Vector3 targetPos;
    private Vector3 startPos;

    private float phaseOffset;

    private float spawnTimer = 0f;
    private bool isSpawning = true;


    void Start()
    {
        targetPos = transform.position;
        startPos = targetPos - new Vector3(0, spawnRiseDistance, 0);
        transform.position = startPos; // Start below the target position

        phaseOffset = Random.Range(0f, 2f * Mathf.PI); // Randomize floating phase
    }

    // Update is called once per frame
    void Update()
    {
        if (isSpawning)
        {
            spawnTimer += Time.deltaTime;
            float t = Mathf.Clamp01(spawnTimer / spawnDuration);
            float curveT = spawnCurve.Evaluate(t);
            transform.position = Vector3.Lerp(startPos, targetPos, curveT);

            if (t >= 1f)
                isSpawning = false; // End spawn animation
        }
        else
        {
           float newY = targetPos.y + Mathf.Sin(Time.time * floatFrequency + phaseOffset) * floatAmplitude;
            transform.position = new Vector3(targetPos.x, newY, targetPos.z);

            transform.Rotate(rotationSpeed * Time.deltaTime);

        }
    }
}
