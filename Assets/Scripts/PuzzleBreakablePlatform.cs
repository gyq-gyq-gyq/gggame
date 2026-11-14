using UnityEngine;

/// <summary>Removes a platform shortly after a player or slime lands on it.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PuzzleBreakablePlatform : MonoBehaviour
{
    [SerializeField, Min(0f)] private float breakDelay = 0.35f;
    private bool breaking;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (breaking || !IsPlayerPart(collision.collider)) return;
        breaking = true;
        Invoke(nameof(Break), breakDelay);
    }

    private void Break() => gameObject.SetActive(false);

    private static bool IsPlayerPart(Collider2D other) =>
        other.GetComponentInParent<PlayerController>() != null ||
        other.GetComponentInParent<JellySoftBody>() != null;
}
