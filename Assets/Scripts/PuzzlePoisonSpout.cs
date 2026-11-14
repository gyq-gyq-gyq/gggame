using UnityEngine;

/// <summary>Periodically enables a 2D poison trigger while its circuit is powered.</summary>
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class PuzzlePoisonSpout : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float activeSeconds = 1.3f;
    [SerializeField, Min(0.1f)] private float inactiveSeconds = 1.1f;

    private BoxCollider2D hazardCollider;
    private SpriteRenderer visual;
    private bool circuitEnabled;
    private bool isActive;

    public void SetCircuitEnabled(bool value) => circuitEnabled = value;

    private void Awake()
    {
        hazardCollider = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        hazardCollider.isTrigger = true;
        SetActive(false);
    }

    private void Update()
    {
        bool nextActive = circuitEnabled && Mathf.Repeat(Time.time, activeSeconds + inactiveSeconds) < activeSeconds;
        SetActive(nextActive);
    }

    private void SetActive(bool value)
    {
        if (isActive == value) return;
        isActive = value;
        if (hazardCollider != null) hazardCollider.enabled = value;
        if (visual != null) visual.color = value ? new Color(0.55f, 0.95f, 0.2f) : new Color(0.2f, 0.35f, 0.16f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive) PuzzleHazard.Respawn(other);
    }
}
