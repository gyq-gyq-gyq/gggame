using UnityEngine;

/// <summary>
/// Lets the slime climb a vertical pole. Holding the horizontal direction
/// toward the pole sticks the slime to its surface and moves it upward;
/// pressing away or releasing input returns control to normal movement.
/// </summary>
public class ClimbablePole : MonoBehaviour
{
    [Header("攀爬触发区")]
    [SerializeField] private Vector2 grabZonePadding = new Vector2(1.2f, 0.3f);

    private Collider2D solidCollider;
    private BoxCollider2D grabZone;

    private void Awake()
    {
        BoxCollider2D[] boxColliders = GetComponents<BoxCollider2D>();
        for (int i = 0; i < boxColliders.Length; i++)
        {
            if (boxColliders[i].isTrigger)
            {
                if (grabZone == null) grabZone = boxColliders[i];
            }
            else if (solidCollider == null)
            {
                solidCollider = boxColliders[i];
            }
        }

        BoxCollider2D solidBox = solidCollider as BoxCollider2D;
        if (solidBox == null)
        {
            solidBox = gameObject.AddComponent<BoxCollider2D>();
            solidBox.size = new Vector2(0.45f, 4.9f);
            solidCollider = solidBox;
        }

        // Keep a real collider for the normal ball and the slime center. A
        // separate, wider trigger handles climbing before the shell reaches a
        // corner. Converting the platform collider itself to a trigger was why
        // GrayClimbPole previously had no physical collision with the ball.
        solidCollider.isTrigger = false;

        if (grabZone == null)
        {
            grabZone = gameObject.AddComponent<BoxCollider2D>();
        }

        grabZone.isTrigger = true;
        grabZone.offset = solidBox.offset;
        grabZone.size = solidBox.size + grabZonePadding;
        grabZone.edgeRadius = solidBox.edgeRadius;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        JellySoftBody jelly = other.GetComponentInParent<JellySoftBody>();
        if (jelly == null || jelly.CenterBody == null || solidCollider == null)
        {
            return;
        }

        // The trigger overlaps the center plus every shell node. Process only
        // the center callback so climbing forces are applied once per step.
        if (other != jelly.CenterCollider)
        {
            return;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float offsetFromPole = jelly.CenterBody.position.x - solidCollider.bounds.center.x;
        float side = Mathf.Sign(offsetFromPole);
        if (Mathf.Approximately(side, 0f)) side = -Mathf.Sign(inputX);

        // Only the direction pressed into the pole activates adhesion. Pressing
        // away immediately releases the slime and restores ordinary movement.
        float directionTowardPole = -side;
        if (inputX * directionTowardPole < 0.1f)
        {
            return;
        }

        jelly.ClimbPole(solidCollider);
    }
}
