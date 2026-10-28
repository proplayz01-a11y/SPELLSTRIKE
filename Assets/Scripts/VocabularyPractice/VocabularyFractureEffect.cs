using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VocabularyFractureEffect : MonoBehaviour
{
    [Header("Fracture Motion")]
    [SerializeField] private Transform explosionOrigin;
    [Min(0f)] [SerializeField] private float explosionForce = 14f;
    [Min(0.1f)] [SerializeField] private float explosionRadius = 35f;
    [Min(0f)] [SerializeField] private float upwardModifier = 1.2f;
    [Min(0f)] [SerializeField] private float randomTorque = 6f;

    [Header("Optional Presentation")]
    [SerializeField] private ParticleSystem fractureVfx;
    [SerializeField] private AudioSource fractureAudio;
    [SerializeField] private bool hidePiecesAfterDelay = true;
    [Min(0.1f)] [SerializeField] private float cleanupDelay = 8f;

    private readonly List<Rigidbody> pieces = new List<Rigidbody>();
    private bool released;

    private void Awake()
    {
        CachePieces();
    }

    private void OnEnable()
    {
        if (released)
            return;

        released = true;
        CachePieces();
        StartCoroutine(ReleaseAfterPhysicsStep());
    }

    private void CachePieces()
    {
        pieces.Clear();
        GetComponentsInChildren(true, pieces);
    }

    private IEnumerator ReleaseAfterPhysicsStep()
    {
        yield return new WaitForFixedUpdate();

        Vector3 origin = explosionOrigin != null
            ? explosionOrigin.position
            : transform.position;

        if (fractureVfx != null)
            fractureVfx.Play(true);
        if (fractureAudio != null)
            fractureAudio.Play();

        for (int i = 0; i < pieces.Count; i++)
        {
            Rigidbody piece = pieces[i];
            if (piece == null)
                continue;

            piece.isKinematic = false;
            piece.useGravity = true;
            piece.WakeUp();
            piece.AddExplosionForce(
                explosionForce,
                origin,
                explosionRadius,
                upwardModifier,
                ForceMode.Impulse);
            piece.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
        }

        Debug.Log($"[VocabularyFractureEffect] Released {pieces.Count} fracture pieces.", this);

        if (!hidePiecesAfterDelay)
            yield break;

        yield return new WaitForSeconds(cleanupDelay);
        gameObject.SetActive(false);
    }
}
