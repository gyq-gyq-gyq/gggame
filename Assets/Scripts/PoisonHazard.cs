using System.Collections;
using UnityEngine;

/// <summary>Alternates a poison trigger and its translucent warning visual.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class PoisonHazard : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float activeSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float inactiveSeconds = 1.5f;
    [SerializeField] private bool startsActive = true;
    [SerializeField] private Color activeColor = new Color(0.55f, 1f, 0.45f, 0.68f);
    [Header("动态激光参数")]
    [SerializeField, Min(0.1f)] private float minBeamLength = 3.5f;
    [SerializeField, Min(0.1f)] private float maxBeamLength = 5.5f;
    [SerializeField, Min(0.1f)] private float minActiveSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float maxActiveSeconds = 2f;
    [SerializeField, Min(0.1f)] private float minInactiveSeconds = 1f;
    [SerializeField, Min(0.1f)] private float maxInactiveSeconds = 1.8f;

    private BoxCollider2D hazardTrigger;
    private SpriteRenderer hazardVisual;
    private Coroutine cycleRoutine;
    private bool isDangerous;
    private float topAnchorWorldY;
    private Vector2 baseVisualSize;
    private Vector2 baseTriggerSize;
    private Vector2 baseTriggerOffset;

    public void Configure(
        bool beginActive,
        float spraySeconds = 1.5f,
        float restSeconds = 1.5f,
        float shortestBeam = 3.5f,
        float longestBeam = 5.5f)
    {
        startsActive = beginActive;
        activeSeconds = Mathf.Max(0.1f, spraySeconds);
        inactiveSeconds = Mathf.Max(0.1f, restSeconds);
        minBeamLength = Mathf.Max(0.1f, shortestBeam);
        maxBeamLength = Mathf.Max(minBeamLength, longestBeam);
    }

    private void Awake()
    {
        hazardTrigger = GetComponent<BoxCollider2D>();
        hazardVisual = GetComponent<SpriteRenderer>();
        hazardTrigger.isTrigger = true;
        baseVisualSize = hazardVisual.size;
        baseTriggerSize = hazardTrigger.size;
        baseTriggerOffset = hazardTrigger.offset;
        topAnchorWorldY = hazardVisual.bounds.max.y;
        SetHazardActive(false);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        cycleRoutine = StartCoroutine(CycleHazard());
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }

        SetHazardActive(false);
    }

    private IEnumerator CycleHazard()
    {
        bool nextState = startsActive;
        while (true)
        {
            SetHazardActive(nextState);
            float duration = nextState
                ? Random.Range(minActiveSeconds, maxActiveSeconds)
                : Random.Range(minInactiveSeconds, maxInactiveSeconds);
            yield return new WaitForSeconds(duration);
            nextState = !nextState;
        }
    }

    private void SetHazardActive(bool value)
    {
        isDangerous = value;
        if (value)
        {
            SetRandomBeamLength();
        }

        if (hazardTrigger != null)
        {
            hazardTrigger.enabled = value;
        }

        if (hazardVisual != null)
        {
            hazardVisual.enabled = value;
            hazardVisual.color = activeColor;
        }
    }

    private void SetRandomBeamLength()
    {
        if (hazardVisual == null || hazardTrigger == null)
        {
            return;
        }

        float length = Random.Range(minBeamLength, maxBeamLength);
        hazardVisual.size = new Vector2(baseVisualSize.x, length);
        float colliderHeight = Mathf.Max(0.05f, length - 0.08f);
        hazardTrigger.size = new Vector2(baseTriggerSize.x, colliderHeight);
        Vector2 colliderOffset = baseTriggerOffset;
        colliderOffset.y += (length - colliderHeight) * 0.5f;
        hazardTrigger.offset = colliderOffset;

        Vector3 position = transform.position;
        position.y = topAnchorWorldY - length * 0.5f;
        transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDangerous)
        {
            SpikeTrap.TryRespawnPlayer(other, "PoisonHazard");
        }
    }

    private void OnValidate()
    {
        activeSeconds = Mathf.Max(0.1f, activeSeconds);
        inactiveSeconds = Mathf.Max(0.1f, inactiveSeconds);
        minBeamLength = Mathf.Max(0.1f, minBeamLength);
        maxBeamLength = Mathf.Max(minBeamLength, maxBeamLength);
        minActiveSeconds = Mathf.Max(0.1f, minActiveSeconds);
        maxActiveSeconds = Mathf.Max(minActiveSeconds, maxActiveSeconds);
        minInactiveSeconds = Mathf.Max(0.1f, minInactiveSeconds);
        maxInactiveSeconds = Mathf.Max(minInactiveSeconds, maxInactiveSeconds);

        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
