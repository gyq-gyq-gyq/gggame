using UnityEngine;

/// <summary>
/// Makes a tree trunk climbable by the normal white ball. The solid collider
/// remains physical; a separate padded trigger provides a forgiving grab area.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class CatTreeClimber : MonoBehaviour
{
    [Header("树干攀爬")]
    [SerializeField, Min(0.1f)] private float climbSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float wallKickHorizontal = 5f;
    [SerializeField, Min(0.1f)] private float wallKickVertical = 6.5f;
    [SerializeField] private Vector2 grabZonePadding = new Vector2(0.9f, 0.3f);
    [SerializeField] private PhysicsMaterial2D climbMaterial;

    private BoxCollider2D solidCollider;
    private BoxCollider2D grabZone;
    private static PhysicsMaterial2D sharedClimbMaterial;

    public float ClimbSpeed => climbSpeed;
    public float WallKickHorizontal => wallKickHorizontal;
    public float WallKickVertical => wallKickVertical;

    /// <summary>
    /// Returns the side of the trunk occupied by the player: -1 is left and
    /// +1 is right. The fallback prevents an ambiguous centre overlap from
    /// making the player switch sides every physics tick.
    /// </summary>
    public int GetAttachmentSide(float playerWorldX, int fallbackSide)
    {
        float offset = playerWorldX - solidCollider.bounds.center.x;
        if (Mathf.Abs(offset) <= 0.01f)
        {
            return fallbackSide == 0 ? 1 : (int)Mathf.Sign(fallbackSide);
        }

        return offset < 0f ? -1 : 1;
    }

    /// <summary>
    /// Gives a collision-free X coordinate immediately beside a selected
    /// trunk side. It is used both for the first grab and for side flips.
    /// </summary>
    public float GetAttachmentX(Collider2D playerCollider, int side)
    {
        float playerHalfWidth = playerCollider == null
            ? 0.5f
            : playerCollider.bounds.extents.x;
        float trunkHalfWidth = solidCollider.bounds.extents.x;
        int normalizedSide = side == 0 ? 1 : (int)Mathf.Sign(side);
        return solidCollider.bounds.center.x +
               normalizedSide * (trunkHalfWidth + playerHalfWidth + 0.02f);
    }

    private void Awake()
    {
        BoxCollider2D[] colliders = GetComponents<BoxCollider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger)
            {
                if (grabZone == null) grabZone = colliders[i];
            }
            else if (solidCollider == null)
            {
                solidCollider = colliders[i];
            }
        }

        if (solidCollider == null)
        {
            solidCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        solidCollider.isTrigger = false;
        solidCollider.sharedMaterial = ResolveClimbMaterial();
        if (grabZone == null)
        {
            grabZone = gameObject.AddComponent<BoxCollider2D>();
        }

        grabZone.isTrigger = true;
        grabZone.offset = solidCollider.offset;
        grabZone.size = solidCollider.size + grabZonePadding;
        grabZone.edgeRadius = solidCollider.edgeRadius;
        grabZone.sharedMaterial = ResolveClimbMaterial();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsMarkedClimbable())
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.currentForm != Form.Normal)
        {
            return;
        }

        player.TryAttachToTree(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.DetachFromTree(this);
        }
    }

    private void OnValidate()
    {
        climbSpeed = Mathf.Max(0.1f, climbSpeed);
        wallKickHorizontal = Mathf.Max(0.1f, wallKickHorizontal);
        wallKickVertical = Mathf.Max(0.1f, wallKickVertical);
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null && collider.isTrigger)
        {
            collider.isTrigger = false;
        }
    }

    private bool IsMarkedClimbable()
    {
        if (gameObject.tag == "ClimbableTree")
        {
            return true;
        }

        int climbableLayer = LayerMask.NameToLayer("ClimbableTree");
        return climbableLayer >= 0 && gameObject.layer == climbableLayer;
    }

    private PhysicsMaterial2D ResolveClimbMaterial()
    {
        if (climbMaterial != null)
        {
            climbMaterial.friction = 0f;
            climbMaterial.bounciness = 0f;
            return climbMaterial;
        }

        if (sharedClimbMaterial == null)
        {
            sharedClimbMaterial = new PhysicsMaterial2D("ClimbableTreeNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        return sharedClimbMaterial;
    }
}
