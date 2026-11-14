using UnityEngine;

/// <summary>2D trigger plate that starts the linked platform and poison cycle.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PuzzlePressurePlate : MonoBehaviour
{
    [SerializeField] private PuzzleMovingPlatform movingPlatform;
    [SerializeField] private PuzzlePoisonSpout poisonSpout;
    private int occupants;

    public void Configure(PuzzleMovingPlatform platform, PuzzlePoisonSpout spout)
    {
        movingPlatform = platform;
        poisonSpout = spout;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerPart(other)) return;
        occupants++;
        RefreshTargets();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerPart(other)) return;
        occupants = Mathf.Max(0, occupants - 1);
        RefreshTargets();
    }

    private void RefreshTargets()
    {
        bool pressed = occupants > 0;
        if (movingPlatform != null) movingPlatform.SetActivated(pressed);
        if (poisonSpout != null) poisonSpout.SetCircuitEnabled(pressed);
    }

    private static bool IsPlayerPart(Collider2D other) =>
        other.GetComponentInParent<PlayerController>() != null ||
        other.GetComponentInParent<JellySoftBody>() != null;
}
