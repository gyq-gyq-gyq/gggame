using System.Collections;
using UnityEngine;

/// <summary>
/// A Flappy Bird-style gate made from a top and bottom laser. Each active
/// cycle chooses a new vertical position for one fixed-size safe opening.
/// </summary>
[DisallowMultipleComponent]
public sealed class LaserGate : MonoBehaviour
{
    [Header("走廊尺寸")]
    public float ceilingY = 9f;
    public float floorY = 0.7f;

    [Header("通道开口设置")]
    public float gapSize = 2f;
    public float minGapCenterY = 2.6f;
    public float maxGapCenterY = 6f;

    [Header("随机节拍")]
    [SerializeField, Min(0.1f)] private float minActiveSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float maxActiveSeconds = 2f;
    [SerializeField, Min(0.1f)] private float minInactiveSeconds = 1f;
    [SerializeField, Min(0.1f)] private float maxInactiveSeconds = 1.8f;
    [SerializeField, Min(0f)] private float initialDelaySeconds;
    [SerializeField] private bool startsActive = true;

    [Header("激光子物体")]
    [SerializeField] private Transform topLaser;
    [SerializeField] private Transform bottomLaser;

    private SpriteRenderer topRenderer;
    private SpriteRenderer bottomRenderer;
    private BoxCollider2D topCollider;
    private BoxCollider2D bottomCollider;
    private Coroutine cycleRoutine;
    private bool isActive;

    public void Configure(
        Transform top,
        Transform bottom,
        float ceilingInsideY,
        float floorInsideY,
        float safeGap,
        float lowestGapCenter,
        float highestGapCenter,
        float initialDelay = 0f,
        bool beginActive = true)
    {
        topLaser = top;
        bottomLaser = bottom;
        ceilingY = ceilingInsideY;
        floorY = floorInsideY;
        gapSize = safeGap;
        minGapCenterY = lowestGapCenter;
        maxGapCenterY = highestGapCenter;
        initialDelaySeconds = Mathf.Max(0f, initialDelay);
        startsActive = beginActive;
    }

    private void Awake()
    {
        ResolveParts();
        ValidateGateRange();
        SetGateActive(false);
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            cycleRoutine = StartCoroutine(GateCycle());
        }
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }

        SetGateActive(false);
    }

    private IEnumerator GateCycle()
    {
        if (initialDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(initialDelaySeconds);
        }

        bool nextState = startsActive;
        while (true)
        {
            SetGateActive(nextState);
            float duration = nextState
                ? Random.Range(minActiveSeconds, maxActiveSeconds)
                : Random.Range(minInactiveSeconds, maxInactiveSeconds);
            yield return new WaitForSeconds(duration);
            nextState = !nextState;
        }
    }

    private void ResolveParts()
    {
        if (topLaser == null) topLaser = transform.Find("TopLaser");
        if (bottomLaser == null) bottomLaser = transform.Find("BottomLaser");

        if (topLaser != null)
        {
            topRenderer = topLaser.GetComponent<SpriteRenderer>();
            topCollider = topLaser.GetComponent<BoxCollider2D>();
        }

        if (bottomLaser != null)
        {
            bottomRenderer = bottomLaser.GetComponent<SpriteRenderer>();
            bottomCollider = bottomLaser.GetComponent<BoxCollider2D>();
        }
    }

    private void SetGateActive(bool value)
    {
        isActive = value;
        if (value)
        {
            ApplyRandomGap();
        }

        SetPartActive(topRenderer, topCollider, value);
        SetPartActive(bottomRenderer, bottomCollider, value);
    }

    private static void SetPartActive(SpriteRenderer renderer, BoxCollider2D collider, bool value)
    {
        if (renderer != null) renderer.enabled = value;
        if (collider != null)
        {
            collider.isTrigger = true;
            collider.enabled = value;
        }
    }

    private void ApplyRandomGap()
    {
        float minimumCenter = Mathf.Max(minGapCenterY, floorY + gapSize * 0.5f);
        float maximumCenter = Mathf.Min(maxGapCenterY, ceilingY - gapSize * 0.5f);
        if (maximumCenter < minimumCenter)
        {
            float midpoint = (floorY + ceilingY) * 0.5f;
            minimumCenter = midpoint;
            maximumCenter = midpoint;
        }

        float gapCenter = Random.Range(minimumCenter, maximumCenter);
        float gapBottom = gapCenter - gapSize * 0.5f;
        float gapTop = gapCenter + gapSize * 0.5f;

        SetBeamGeometry(topLaser, topRenderer, topCollider, ceilingY - gapTop,
            (ceilingY + gapTop) * 0.5f);
        SetBeamGeometry(bottomLaser, bottomRenderer, bottomCollider, gapBottom - floorY,
            (floorY + gapBottom) * 0.5f);
    }

    private void SetBeamGeometry(
        Transform beam,
        SpriteRenderer renderer,
        BoxCollider2D collider,
        float rawLength,
        float worldCenterY)
    {
        if (beam == null || renderer == null || collider == null)
        {
            return;
        }

        float length = Mathf.Max(0.05f, rawLength);
        renderer.size = new Vector2(renderer.size.x, length);
        collider.size = new Vector2(collider.size.x, length);
        collider.offset = Vector2.zero;

        Vector3 localPosition = beam.localPosition;
        localPosition.y = worldCenterY - transform.position.y;
        beam.localPosition = localPosition;
    }

    private void ValidateGateRange()
    {
        gapSize = Mathf.Max(0.1f, gapSize);
        minGapCenterY = Mathf.Min(minGapCenterY, maxGapCenterY);
        maxGapCenterY = Mathf.Max(minGapCenterY, maxGapCenterY);
        minActiveSeconds = Mathf.Max(0.1f, minActiveSeconds);
        maxActiveSeconds = Mathf.Max(minActiveSeconds, maxActiveSeconds);
        minInactiveSeconds = Mathf.Max(0.1f, minInactiveSeconds);
        maxInactiveSeconds = Mathf.Max(minInactiveSeconds, maxInactiveSeconds);
    }

    private void OnValidate()
    {
        ValidateGateRange();
    }

    internal void HitPlayer(Collider2D other)
    {
        if (isActive)
        {
            SpikeTrap.TryRespawnPlayer(other, "LaserGate");
        }
    }
}

/// <summary>Forwards child beam trigger hits to the owning LaserGate.</summary>
[DisallowMultipleComponent]
public sealed class LaserGateBeam : MonoBehaviour
{
    private LaserGate gate;

    private void Awake()
    {
        gate = GetComponentInParent<LaserGate>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gate != null)
        {
            gate.HitPlayer(other);
        }
    }
}
