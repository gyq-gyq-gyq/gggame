using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grants the normal player body Flappy Bird-style flight while it overlaps
/// this trigger. The state is reference-counted so multiple player colliders
/// cannot prematurely disable the zone on exit.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class InfiniteFlapZone : MonoBehaviour
{
    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private Collider2D zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        playerColliders.Add(other);
        player.isInInfiniteFlightZone = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        playerColliders.Remove(other);
        if (playerColliders.Count == 0)
        {
            player.isInInfiniteFlightZone = false;
        }
    }

    private void OnDisable()
    {
        foreach (Collider2D collider in playerColliders)
        {
            if (collider == null)
            {
                continue;
            }

            PlayerController player = collider.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.isInInfiniteFlightZone = false;
            }
        }

        playerColliders.Clear();
    }

    private void OnValidate()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
