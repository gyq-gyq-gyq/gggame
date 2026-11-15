using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 要跟随的物体（玩家）

    [Header("跟随参数")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private Vector2 deadZone = new Vector2(1.25f, 0.8f);
    [SerializeField, Min(0f)] private float settleDistance = 0.005f;

    [Header("画面")]
    [SerializeField] private float orthographicSize = 5.5f;
    [SerializeField] private bool keepGroundVisible = true;
    [SerializeField] private float groundTopY = -2.65f;
    [SerializeField] private float groundMargin = 0.25f;

    private Vector3 velocity = Vector3.zero;
    private Transform previousTarget;

    private void Awake()
    {
        if (target == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        Camera cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null && cameraComponent.orthographic)
        {
            cameraComponent.orthographicSize = orthographicSize;
        }
    }

    /// <summary>
    /// Changes the tracked object and clears the velocity left over from the
    /// previous target. This prevents a form switch from kicking the camera.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        previousTarget = newTarget;
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // The target is public for inspector compatibility, so also handle
        // target changes that did not go through SetTarget.
        if (target != previousTarget)
        {
            previousTarget = target;
            velocity = Vector3.zero;
        }

        Vector3 targetPosition = transform.position;
        float targetX = target.position.x + offset.x;
        float targetY = target.position.y + offset.y;

        // 目标在自由区域内时保持镜头位置，超出后才开始跟随。
        float cameraTargetX = transform.position.x - offset.x;
        float cameraTargetY = transform.position.y - offset.y;
        bool followX = Mathf.Abs(target.position.x - cameraTargetX) > deadZone.x;
        bool followY = Mathf.Abs(target.position.y - cameraTargetY) > deadZone.y;

        if (followX)
        {
            targetPosition.x = targetX - Mathf.Sign(target.position.x - cameraTargetX) * deadZone.x;
        }
        else
        {
            // SmoothDamp keeps using its old velocity even when its destination
            // becomes the current position. Clear that remainder so a resting
            // player cannot make the camera oscillate around a dead-zone edge.
            velocity.x = 0f;
        }

        if (followY)
        {
            targetPosition.y = targetY - Mathf.Sign(target.position.y - cameraTargetY) * deadZone.y;
        }
        else
        {
            velocity.y = 0f;
        }

        targetPosition.z = offset.z;

        Camera cameraComponent = GetComponent<Camera>();
        if (keepGroundVisible && cameraComponent != null && cameraComponent.orthographic)
        {
            // Keep the top edge of the ground below the camera's bottom edge.
            float maximumY = groundTopY + cameraComponent.orthographicSize - groundMargin;
            targetPosition.y = Mathf.Min(targetPosition.y, maximumY);
        }

        if ((targetPosition - transform.position).sqrMagnitude <= settleDistance * settleDistance)
        {
            transform.position = targetPosition;
            velocity = Vector3.zero;
            return;
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );

        if (keepGroundVisible && cameraComponent != null && cameraComponent.orthographic)
        {
            float maximumY = groundTopY + cameraComponent.orthographicSize - groundMargin;
            smoothedPosition.y = Mathf.Min(smoothedPosition.y, maximumY);
        }

        transform.position = smoothedPosition;
    }
}
