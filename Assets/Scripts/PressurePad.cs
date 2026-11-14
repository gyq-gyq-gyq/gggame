using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A latching pressure pad. The pad sinks while a player form overlaps it and
/// permanently starts the linked moving platform on the first valid press.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PressurePad : MonoBehaviour
{
    [SerializeField] private MovingPlatform movingPlatform;
    [SerializeField] private Transform visualTransform;
    [SerializeField, Min(0f)] private float pressedDepth = 0.1f;
    [SerializeField, Min(0.01f)] private float visualSpeed = 0.8f;

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
    private Vector3 visualRestLocalPosition;

    public void Configure(MovingPlatform target, Transform padVisual = null)
    {
        movingPlatform = target;
        if (padVisual != null)
        {
            visualTransform = padVisual;
        }
    }

    private void Awake()
    {
        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        visualRestLocalPosition = visualTransform.localPosition;
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (visualTransform == null)
        {
            return;
        }

        Vector3 target = visualRestLocalPosition;
        if (occupants.Count > 0)
        {
            target.y -= pressedDepth;
        }

        visualTransform.localPosition = Vector3.MoveTowards(
            visualTransform.localPosition,
            target,
            visualSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!SpikeTrap.IsPlayerCollider(other) || !occupants.Add(other))
        {
            return;
        }

        if (movingPlatform != null)
        {
            movingPlatform.Activate();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        occupants.Remove(other);
    }

    private void OnDisable()
    {
        occupants.Clear();
        if (visualTransform != null)
        {
            visualTransform.localPosition = visualRestLocalPosition;
        }
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
