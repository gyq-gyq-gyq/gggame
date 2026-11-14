using UnityEngine;
using UnityEngine.Events;

/// <summary>One-shot goal trigger with an optional victory panel and event.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class GoalPortal : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private UnityEvent onGoalReached;
    [SerializeField] private Transform glowVisual;
    [SerializeField, Min(0f)] private float pulseSpeed = 2.5f;
    [SerializeField, Range(0f, 0.3f)] private float pulseAmount = 0.12f;

    private Vector3 glowBaseScale = Vector3.one;
    private bool completed;

    public void Configure(Transform glow)
    {
        glowVisual = glow;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (glowVisual != null)
        {
            glowBaseScale = glowVisual.localScale;
        }
    }

    private void Update()
    {
        if (glowVisual == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        glowVisual.localScale = glowBaseScale * pulse;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed || !SpikeTrap.IsPlayerCollider(other))
        {
            return;
        }

        completed = true;
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        onGoalReached?.Invoke();
        Debug.Log("GoalPortal: puzzle level completed!");
    }

    private void OnValidate()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
