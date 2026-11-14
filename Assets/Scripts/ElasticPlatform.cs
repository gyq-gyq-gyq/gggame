using System.Collections.Generic;
using UnityEngine;

public class ElasticPlatform : MonoBehaviour
{
    [Header("史莱姆弹跳")]
    [SerializeField, Min(0.1f)] private float bounceForce = 16f;
    [SerializeField, Min(1f)] private float bounceMultiplier = 1.3f;
    [SerializeField, Min(0f)] private float minimumImpactVelocity = 0f;
    [SerializeField] private float horizontalBoost = 4f;
    [SerializeField, Min(0f)] private float bounceCooldown = 0.25f;

    private readonly Dictionary<JellySoftBody, float> lastBounceTime = new Dictionary<JellySoftBody, float>();
    private Collider2D platformCollider;

    public void Configure(float force, float multiplier, float minimumImpactVelocity = 0f)
    {
        bounceForce = Mathf.Max(0.1f, force);
        bounceMultiplier = Mathf.Max(1f, multiplier);
        this.minimumImpactVelocity = Mathf.Max(0f, minimumImpactVelocity);
    }

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        JellySoftBody jelly = collision.collider.GetComponentInParent<JellySoftBody>();
        if (jelly == null || !HitFromAbove(collision) ||
            jelly.GetCenterVelocity().y > minimumImpactVelocity)
        {
            return;
        }

        if (lastBounceTime.TryGetValue(jelly, out float lastTime) && Time.time - lastTime < bounceCooldown)
        {
            return;
        }

        lastBounceTime[jelly] = Time.time;
        jelly.Bounce(bounceForce * bounceMultiplier, horizontalBoost);
    }

    private bool HitFromAbove(Collision2D collision)
    {
        if (platformCollider == null)
        {
            return false;
        }

        float top = platformCollider.bounds.max.y - 0.08f;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.point.y >= top)
            {
                return true;
            }
        }

        return false;
    }
}
