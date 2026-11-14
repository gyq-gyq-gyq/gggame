using UnityEngine;

/// <summary>2D portal used for the start and the level exit.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PuzzlePortal : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private bool isExit;

    public void Configure(Transform target, bool exitPortal)
    {
        destination = target;
        isExit = exitPortal;
    }

    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        JellySoftBody jelly = other.GetComponentInParent<JellySoftBody>();
        Transform target = player != null ? player.transform : jelly != null ? jelly.transform : null;
        if (target == null || destination == null) return;

        target.position = destination.position;
        if (isExit) Debug.Log("PuzzleLevel: Exit portal reached.");
    }
}
