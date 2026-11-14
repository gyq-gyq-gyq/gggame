using UnityEngine;

/// <summary>Activated kinematic platform that moves between two world anchors.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public sealed class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField, Min(0.01f)] private float moveSpeed = 2.5f;
    [SerializeField] private bool isActivated;

    private Rigidbody2D body;
    private bool movingTowardB = true;

    public bool IsActivated => isActivated;

    public void Configure(Transform firstPoint, Transform secondPoint, float speed)
    {
        pointA = firstPoint;
        pointB = secondPoint;
        moveSpeed = Mathf.Max(0.01f, speed);
    }

    public void Activate()
    {
        isActivated = true;
        if (body != null)
        {
            body.WakeUp();
        }
    }

    public void Deactivate()
    {
        isActivated = false;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void FixedUpdate()
    {
        if (!isActivated || pointA == null || pointB == null)
        {
            return;
        }

        Vector2 destination = movingTowardB ? pointB.position : pointA.position;
        Vector2 nextPosition = Vector2.MoveTowards(
            body.position,
            destination,
            moveSpeed * Time.fixedDeltaTime);

        body.MovePosition(nextPosition);
        if ((nextPosition - destination).sqrMagnitude <= 0.0001f)
        {
            movingTowardB = !movingTowardB;
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
    }
}
