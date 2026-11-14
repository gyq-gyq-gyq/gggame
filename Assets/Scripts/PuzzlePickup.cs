using UnityEngine;

/// <summary>Visual-only pickup anchor. It does not alter player forms or stats.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PuzzlePickup : MonoBehaviour
{
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null || other.GetComponentInParent<JellySoftBody>() != null)
        {
            gameObject.SetActive(false);
        }
    }
}
