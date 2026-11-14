using UnityEngine;

public class StickyBoard : MonoBehaviour
{
    [Header("史莱姆墙面粘附")]
    [SerializeField, Min(0.05f)] private float stickDuration = 0.7f;
    [SerializeField] private float maxSlideSpeed = -1f;
    [SerializeField, Range(0f, 1f)] private float minimumWallNormal = 0.6f;

    private void OnCollisionStay2D(Collision2D collision)
    {
        JellySoftBody jelly = collision.collider.GetComponentInParent<JellySoftBody>();
        if (jelly == null || !TouchesVerticalSurface(collision))
        {
            return;
        }

        jelly.StickToWall(stickDuration, maxSlideSpeed);
    }

    private bool TouchesVerticalSurface(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Green platform tops remain ordinary landing surfaces; only the
            // side faces act as sticky walls.
            if (Mathf.Abs(contact.normal.x) >= minimumWallNormal)
            {
                return true;
            }
        }

        return false;
    }
}
