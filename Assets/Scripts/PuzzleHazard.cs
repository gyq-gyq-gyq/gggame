using UnityEngine;

/// <summary>Shared trigger hazard; moves the current player form back to PlayerSpawn.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class PuzzleHazard : MonoBehaviour
{
    private static Transform spawnPoint;

    public static void SetSpawnPoint(Transform point) => spawnPoint = point;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => Respawn(other);

    public static void Respawn(Collider2D other)
    {
        if (spawnPoint == null)
        {
            GameObject spawnObject = GameObject.Find("PlayerSpawn");
            if (spawnObject != null) spawnPoint = spawnObject.transform;
        }
        if (spawnPoint == null) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        JellySoftBody jelly = other.GetComponentInParent<JellySoftBody>();
        Transform target = player != null ? player.transform : jelly != null ? jelly.transform : null;
        if (target == null) return;

        target.position = spawnPoint.position;
        Rigidbody2D[] bodies = target.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++) bodies[i].velocity = Vector2.zero;
    }
}
