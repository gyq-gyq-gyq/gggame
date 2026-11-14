using UnityEngine;

/// <summary>
/// Locked level exit. It becomes passable only after every key is collected.
/// </summary>
public class LevelExitDoor : MonoBehaviour
{
    [SerializeField] private Color lockedColor = new Color(0.52f, 0.32f, 0.72f);
    [SerializeField] private Color openColor = new Color(0.26f, 0.95f, 0.58f);

    private Collider2D blockingCollider;
    private SpriteRenderer visual;
    private bool opened;

    private void Awake()
    {
        blockingCollider = GetComponent<Collider2D>();
        visual = GetComponent<SpriteRenderer>();
        if (visual != null)
        {
            visual.color = lockedColor;
        }

        BoxCollider2D goalTrigger = gameObject.AddComponent<BoxCollider2D>();
        goalTrigger.isTrigger = true;
        if (blockingCollider is BoxCollider2D box)
        {
            goalTrigger.size = box.size + new Vector2(0.4f, 0.4f);
            goalTrigger.offset = box.offset;
        }
    }

    private void Update()
    {
        if (!opened && KeyDoorManager.Instance != null && KeyDoorManager.Instance.HasAllKeys)
        {
            opened = true;
            if (blockingCollider != null)
            {
                blockingCollider.enabled = false;
            }
            if (visual != null)
            {
                visual.color = openColor;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened && BelongsToPlayer(other) && KeyDoorManager.Instance != null)
        {
            KeyDoorManager.Instance.CompleteLevel();
        }
    }

    private static bool BelongsToPlayer(Collider2D collider)
    {
        return collider.GetComponentInParent<PlayerController>() != null ||
               collider.GetComponentInParent<JellySoftBody>() != null;
    }
}
