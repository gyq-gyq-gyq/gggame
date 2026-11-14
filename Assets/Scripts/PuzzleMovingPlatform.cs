using UnityEngine;

/// <summary>2D-only kinematic platform, activated by the pressure plate.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PuzzleMovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 travel = new Vector2(0f, 3f);
    [SerializeField, Min(0.1f)] private float speed = 2.2f;
    [SerializeField] private bool activeOnStart;

    private Rigidbody2D body;
    private Vector2 start;
    private bool activated;

    public void Configure(Vector2 movement, float movementSpeed)
    {
        travel = movement;
        speed = movementSpeed;
    }

    public void SetActivated(bool value) => activated = value;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        start = body.position;
        activated = activeOnStart;
    }

    private void FixedUpdate()
    {
        if (!activated)
        {
            return;
        }

        float phase = Mathf.PingPong(Time.time * speed, 1f);
        body.MovePosition(Vector2.Lerp(start, start + travel, phase));
    }
}
