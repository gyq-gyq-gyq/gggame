using UnityEngine;

/// <summary>Respawns either the normal player body or the active jelly body.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class SpikeTrap : MonoBehaviour
{
    private static int lastRespawnTargetId;
    private static float lastRespawnTime = float.NegativeInfinity;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRespawnPlayer(other, name);
    }

    public static bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        return other.GetComponentInParent<PlayerController>() != null ||
               other.GetComponentInParent<JellySoftBody>() != null;
    }

    public static bool TryRespawnPlayer(Collider2D other, string sourceName)
    {
        if (!TryGetPlayerRoot(other, out Transform playerRoot))
        {
            return false;
        }

        int targetId = playerRoot.GetInstanceID();
        if (lastRespawnTargetId == targetId && Time.unscaledTime - lastRespawnTime < 0.15f)
        {
            return true;
        }

        GameObject spawnObject = GameObject.Find("PlayerSpawn");
        if (spawnObject == null)
        {
            Debug.LogWarning($"{sourceName}: player touched a hazard, but no PlayerSpawn exists.");
            return true;
        }

        lastRespawnTargetId = targetId;
        lastRespawnTime = Time.unscaledTime;

        playerRoot.position = spawnObject.transform.position;
        Rigidbody2D[] bodies = playerRoot.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].velocity = Vector2.zero;
            bodies[i].angularVelocity = 0f;
            bodies[i].WakeUp();
        }

        Physics2D.SyncTransforms();
        Debug.Log($"{sourceName}: player respawned at PlayerSpawn.");
        return true;
    }

    public static bool TryGetPlayerRoot(Collider2D other, out Transform playerRoot)
    {
        playerRoot = null;
        if (other == null)
        {
            return false;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            playerRoot = player.transform;
            return true;
        }

        JellySoftBody jelly = other.GetComponentInParent<JellySoftBody>();
        if (jelly != null)
        {
            playerRoot = jelly.transform;
            return true;
        }

        return false;
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
