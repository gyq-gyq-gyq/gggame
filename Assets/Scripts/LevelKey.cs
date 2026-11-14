using UnityEngine;

/// <summary>
/// Collectible key. Both the normal player body and any slime edge node can collect it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelKey : MonoBehaviour
{
    private bool collected;

    private void Awake()
    {
        if (KeyDoorManager.Instance != null)
        {
            KeyDoorManager.Instance.RegisterKey();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !BelongsToPlayer(other) || KeyDoorManager.Instance == null)
        {
            return;
        }

        collected = true;
        KeyDoorManager.Instance.CollectKey();
        Destroy(gameObject);
    }

    private static bool BelongsToPlayer(Collider2D collider)
    {
        return collider.GetComponentInParent<PlayerController>() != null ||
               collider.GetComponentInParent<JellySoftBody>() != null;
    }
}
