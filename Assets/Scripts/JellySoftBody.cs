using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(JellyRenderer))]
public class JellySoftBody : MonoBehaviour
{
    [Header("Body Shape")]
    [SerializeField, Min(3)] private int edgeNodeCount = 12;
    [SerializeField, Min(0.05f)] private float radius = 0.65f;
    [SerializeField, Min(0.01f)] private float centerColliderRadius = 0.12f;
    [SerializeField, Range(0.08f, 0.1f)] private float edgeColliderRadius = 0.09f;
    [SerializeField, Min(1f)] private float maxStretchMultiplier = 1.65f;

    [Header("Corner Slide Assist")]
    [SerializeField, Min(0f)] private float cornerSlideForce = 2.5f;
    [SerializeField, Range(0.05f, 0.7f)] private float cornerNormalMinComponent = 0.25f;
    [SerializeField, Min(1f)] private float cornerStretchThreshold = 1.04f;
    [SerializeField, Min(0.01f)] private float cornerStuckSpeed = 0.4f;

    [Header("Spring Settings")]
    // SpringJoint2D frequency is stiffness: lower values are more fluid and
    // allow the shell to yield before the centre resists compression.
    [SerializeField, Range(2f, 8f)] private float springFrequency = 3.2f;
    [SerializeField, Range(0.01f, 1f)] private float springDampingRatio = 0.6f;
    [SerializeField, Range(1f, 2f)] private float shellFrequencyMultiplier = 1.1f;
    [SerializeField, Range(0.1f, 2f)] private float shellDampingMultiplier = 0.9f;
    [Header("Cross-bracing Springs")]
    [SerializeField, Range(0f, 2f)] private float crossFrequencyMultiplier = 0.5f;
    [SerializeField, Range(0f, 2f)] private float crossDampingMultiplier = 0.5f;
    
    [Header("Pressure Soft-Body")]
    [SerializeField, Min(0f)] private float pressureMultiplier = 80f;
    [SerializeField, Range(0f, 0.1f)] private float pressureDeadZone = 0.02f;
    [SerializeField, Range(1f, 1.5f)] private float pressureResponseExponent = 1.15f;
    [SerializeField, Range(0.1f, 1f)] private float compressedVerticalPressureScale = 0.25f;
    [SerializeField, Range(1f, 3f)] private float lateralFlowMultiplier = 2f;
    [SerializeField, Range(0.2f, 1f)] private float verticalCompressionAspect = 0.7f;

    [Header("Climbing Motion")]
    [SerializeField, Min(0f)] private float climbForce = 28f;
    [SerializeField, Min(0.01f)] private float climbFrequency = 5f;
    [SerializeField, Min(0f)] private float adhesionForce = 55f;
    [SerializeField, Range(2, 3)] private int climbGripNodeCount = 3;
    [SerializeField, Min(0.05f)] private float climbGripDistance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float climbGravitySupport = 1f;
    [SerializeField, Min(0f)] private float climbCorePullForce = 18f;
    private float targetArea;
    
    [Header("Rigidbody Settings")]
    [SerializeField, Min(0.1f)] private float gravityScale = 1.5f;
    [SerializeField, Min(0.01f)] private float centerMass = 1.2f;
    [SerializeField, Min(0.01f)] private float edgeMass = 0.6f;
    [SerializeField, Min(0f)] private float centerLinearDrag = 0.8f;
    [SerializeField, Min(0f)] private float edgeLinearDrag = 0.35f;
    [SerializeField, Min(0f)] private float angularDrag = 3f;
    [SerializeField] private RigidbodyInterpolation2D interpolation = RigidbodyInterpolation2D.Interpolate;
    [SerializeField] private CollisionDetectionMode2D collisionDetection = CollisionDetectionMode2D.Continuous;
    [SerializeField] private string bodyLayer = "JellyPart";

    [Header("Super Bounce")]
    [SerializeField] private float bounceMultiplier = 1.3f; // 动能放大倍数
    [SerializeField] private float minBounceVelocity = 3f; // 触发反弹的最小下坠速度

    [Header("Motion")]
    [SerializeField, Min(0.1f)] private float moveForce = 12f;
    [SerializeField, Min(0.1f)] private float maxMoveSpeed = 6f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Squeeze Crawl Assist")]
    [SerializeField] private bool enableSqueezeCrawlAssist = true;
    [SerializeField, Min(0f)] private float crawlPullForce = 18f;
    [SerializeField, Min(0.01f)] private float crawlBlockedSpeed = 0.2f;
    [SerializeField, Range(2, 3)] private int crawlLeadNodeCount = 3;
    [SerializeField, Range(0.05f, 1f)] private float crawlLowerHalfHeight = 0.5f;
    [SerializeField, Range(0.05f, 1f)] private float crawlRadialDampingMultiplier = 0.35f;
    [SerializeField, Min(0.01f)] private float crawlInputTimeout = 0.12f;

    private readonly List<Transform> _edgeNodes = new List<Transform>();
    private readonly List<Rigidbody2D> _edgeBodies = new List<Rigidbody2D>();
    private readonly List<CircleCollider2D> _edgeColliders = new List<CircleCollider2D>();
    private readonly List<SpringJoint2D> _radialSprings = new List<SpringJoint2D>();
    private readonly List<float> _radialDamping = new List<float>();
    private readonly ContactPoint2D[] _cornerContacts = new ContactPoint2D[8];
    private readonly int[] _climbNodeIndices = new int[3];
    private readonly float[] _climbNodeDistances = new float[3];
    private Rigidbody2D _centerBody;
    private CircleCollider2D _centerCollider;
    private PhysicsMaterial2D _noFrictionMaterial;
    private ContactFilter2D _solidContactFilter;
    private bool _built;
    private bool _wasGrounded;
    private bool _bounceArmed = true;
    private float _lastVelocityY;
    private float _stickyUntil;
    private float _stickySlideSpeed = -1f;
    private float _lastMoveInput;
    private float _lastMoveInputTime = float.NegativeInfinity;

    public Rigidbody2D CenterBody => _centerBody;
    public CircleCollider2D CenterCollider => _centerCollider;
    public int EdgeNodeCount => _edgeNodes.Count;
    public IReadOnlyList<Transform> EdgeNodes => _edgeNodes;

    public bool IsGrounded
    {
        get
        {
            if (_centerCollider == null)
            {
                return false;
            }

            if (_centerCollider.IsTouchingLayers(groundLayers))
            {
                return true;
            }

            // The visible shell is the primary support surface. Any solid edge
            // node touching a configured ground layer counts as grounded.
            for (int i = 0; i < _edgeColliders.Count; i++)
            {
                Collider2D edgeCollider = _edgeColliders[i];
                if (edgeCollider != null &&
                    edgeCollider.enabled &&
                    !edgeCollider.isTrigger &&
                    edgeCollider.IsTouchingLayers(groundLayers))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        BuildBody();
        _solidContactFilter = new ContactFilter2D();
        _solidContactFilter.NoFilter();
        _solidContactFilter.useTriggers = false;
        targetArea = GetSignedPolygonArea();
    }

    private void FixedUpdate()
    {
        if (_centerBody == null) return;

        // 1. 限制中心节点速度
        Vector2 velocity = _centerBody.velocity;
        float clampedCenterX = Mathf.Clamp(velocity.x, -maxMoveSpeed, maxMoveSpeed);
        if (!Mathf.Approximately(clampedCenterX, velocity.x))
        {
            velocity.x = clampedCenterX;
            _centerBody.velocity = velocity;
        }

        // 【新增修复】2. 同步限制外围节点的速度！
        // 防止外皮在惯性下跑得太快或太慢，导致整个果冻被扯扁
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            Vector2 edgeVel = _edgeBodies[i].velocity;
            // 外围节点允许比中心快一点点以保留 Q 弹感，但不能无限拉伸
            float clampedEdgeX = Mathf.Clamp(edgeVel.x, -maxMoveSpeed * 1.2f, maxMoveSpeed * 1.2f);
            if (!Mathf.Approximately(clampedEdgeX, edgeVel.x))
            {
                edgeVel.x = clampedEdgeX;
                _edgeBodies[i].velocity = edgeVel;
            }
        }

        if (Time.time < _stickyUntil)
        {
            ApplyStickyWallMotion(_centerBody);
            for (int i = 0; i < _edgeBodies.Count; i++)
            {
                ApplyStickyWallMotion(_edgeBodies[i]);
            }
        }

        RestoreRadialSpringDamping();
        ApplySqueezeCrawlAssist();
        ApplyCornerSlideAssist();
        ApplyGasPressure();

        bool currentlyGrounded = IsGrounded;
        if (!_wasGrounded && currentlyGrounded && _bounceArmed && _lastVelocityY < -minBounceVelocity)
        {
            float bounceVel = Mathf.Abs(_lastVelocityY) * bounceMultiplier;

            Vector2 centerVelocity = _centerBody.velocity;
            centerVelocity.y = bounceVel;
            _centerBody.velocity = centerVelocity;

            for (int i = 0; i < _edgeBodies.Count; i++)
            {
                Vector2 edgeVelocity = _edgeBodies[i].velocity;
                edgeVelocity.y = bounceVel;
                _edgeBodies[i].velocity = edgeVelocity;
            }

            // One artificial bounce per airborne-to-grounded cycle.
            _bounceArmed = false;
        }

        // Arm the next bounce only after the slime has genuinely left the ground.
        if (!currentlyGrounded)
        {
            _bounceArmed = true;
        }

        _wasGrounded = currentlyGrounded;
        _lastVelocityY = _centerBody.velocity.y;
    }

    private void OnDestroy()
    {
        if (_noFrictionMaterial != null)
        {
            Destroy(_noFrictionMaterial);
            _noFrictionMaterial = null;
        }
    }

    public bool TryGetEdgeLocalPosition(int index, out Vector3 localPosition)
    {
        if (index < 0 || index >= _edgeNodes.Count)
        {
            localPosition = Vector3.zero;
            return false;
        }

        localPosition = transform.InverseTransformPoint(_edgeNodes[index].position);
        return true;
    }

    public Vector3 GetEdgeLocalPosition(int index)
    {
        if (index < 0 || index >= _edgeNodes.Count)
        {
            return Vector3.zero;
        }

        return transform.InverseTransformPoint(_edgeNodes[index].position);
    }

    public Vector2 GetCenterVelocity()
    {
        return _centerBody == null ? Vector2.zero : _centerBody.velocity;
    }

    public void SetCenterVelocity(Vector2 velocity)
    {
        if (_centerBody != null)
        {
            _centerBody.velocity = velocity;

            // Keep the shell moving with the center when switching forms so
            // the first frame does not leave the slime suspended or stretched.
            for (int i = 0; i < _edgeBodies.Count; i++)
            {
                _edgeBodies[i].velocity = velocity;
            }
        }
    }

    /// <summary>
    /// Launches every node together so a bounce pad cannot pull one shell node
    /// away from the rest of the soft body.
    /// </summary>
    public void Bounce(float verticalSpeed, float horizontalBoost = 0f)
    {
        if (_centerBody == null || verticalSpeed <= 0f)
        {
            return;
        }

        ApplyVelocityToBody(_centerBody, verticalSpeed, horizontalBoost);
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            ApplyVelocityToBody(_edgeBodies[i], verticalSpeed, horizontalBoost);
        }

        // The pad supplies this jump. Prevent the automatic ground-bounce
        // from being stacked while shell nodes are still in contact.
        _bounceArmed = false;
        _wasGrounded = true;
        _lastVelocityY = verticalSpeed;
    }

    /// <summary>
    /// Briefly limits falling speed while touching a sticky vertical wall.
    /// It is a slide, not a freeze, so leaving the wall works naturally.
    /// </summary>
    public void StickToWall(float duration, float maxSlideSpeed)
    {
        if (_centerBody == null || duration <= 0f)
        {
            return;
        }

        _stickyUntil = Mathf.Max(_stickyUntil, Time.time + duration);
        _stickySlideSpeed = Mathf.Min(maxSlideSpeed, -0.01f);
    }

    public void Move(float direction)
    {
        if (_centerBody == null) return;

        float input = Mathf.Clamp(direction, -1f, 1f);
        _lastMoveInput = input;
        _lastMoveInputTime = Time.time;
        if (Mathf.Abs(input) < 0.001f) return;

        // 计算总推力
        Vector2 force = new Vector2(input * moveForce, 0f);

        // 【核心修复】：分散受力以保持体积守恒 (Volume Conservation)
        // 中心点承受 50% 的移动力
        _centerBody.AddForce(force * 0.5f, ForceMode2D.Force);
        
        // 剩下的 50% 的力，均匀地分配给外皮的 12 个节点
        Vector2 edgeForce = (force * 0.5f) / _edgeBodies.Count;
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            _edgeBodies[i].AddForce(edgeForce, ForceMode2D.Force);
        }
    }

    public void Jump(float force)
    {
        if (_centerBody == null || force <= 0f)
        {
            return;
        }

        _centerBody.AddForce(new Vector2(0f, force), ForceMode2D.Impulse);
    }

    public bool TryJump(float force)
    {
        if (!IsGrounded)
        {
            return false;
        }

        Jump(force);
        return true;
    }

    /// <summary>
    /// Physically grips a pole with the nearest shell nodes. Out-of-phase
    /// upward pulses deform the attached side first; the spring network then
    /// pulls the core and opposite side upward like a peristaltic crawl.
    /// </summary>
    public void ClimbPole(Collider2D poleCollider)
    {
        if (_centerBody == null || poleCollider == null || _edgeBodies.Count == 0)
        {
            return;
        }

        int requestedGripCount = Mathf.Min(climbGripNodeCount, _climbNodeIndices.Length, _edgeBodies.Count);
        for (int i = 0; i < requestedGripCount; i++)
        {
            _climbNodeIndices[i] = -1;
            _climbNodeDistances[i] = float.PositiveInfinity;
        }

        float maxGripDistanceSqr = climbGripDistance * climbGripDistance;
        for (int edgeIndex = 0; edgeIndex < _edgeBodies.Count; edgeIndex++)
        {
            Vector2 edgePosition = _edgeBodies[edgeIndex].position;
            Vector2 closestPoint = poleCollider.ClosestPoint(edgePosition);
            float distanceSqr = (closestPoint - edgePosition).sqrMagnitude;
            if (distanceSqr > maxGripDistanceSqr)
            {
                continue;
            }

            InsertClimbCandidate(edgeIndex, distanceSqr, requestedGripCount);
        }

        int activeGripCount = 0;
        while (activeGripCount < requestedGripCount && _climbNodeIndices[activeGripCount] >= 0)
        {
            activeGripCount++;
        }

        if (activeGripCount == 0)
        {
            return;
        }

        SortClimbNodesBottomToTop(activeGripCount);

        float totalMass = _centerBody.mass;
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            totalMass += _edgeBodies[i].mass;
        }

        Vector2 gravitySupport = -Physics2D.gravity * gravityScale *
            (totalMass * climbGravitySupport / activeGripCount);
        float phaseStep = Mathf.PI * 2f / activeGripCount;
        float pulseSum = 0f;

        for (int rank = 0; rank < activeGripCount; rank++)
        {
            Rigidbody2D gripBody = _edgeBodies[_climbNodeIndices[rank]];
            Vector2 gripPosition = gripBody.position;
            Vector2 surfacePoint = poleCollider.ClosestPoint(gripPosition);
            Vector2 towardSurface = surfacePoint - gripPosition;

            if (towardSurface.sqrMagnitude > 0.000001f)
            {
                float distance = towardSurface.magnitude;
                float adhesionScale = 1f - Mathf.Clamp01(distance / climbGripDistance);
                gripBody.AddForce(
                    towardSurface / distance * adhesionForce * Mathf.Lerp(0.5f, 1f, adhesionScale),
                    ForceMode2D.Force);
            }

            // Squaring the positive half-wave creates a distinct grip/pull/
            // release rhythm. The phase offset makes lower, middle and upper
            // nodes pulse in sequence instead of translating as one rigid mass.
            float wave = Mathf.Sin(Time.fixedTime * climbFrequency + rank * phaseStep);
            float pulse = Mathf.Max(0f, wave);
            pulse *= pulse;
            pulseSum += pulse;

            gripBody.AddForce(gravitySupport, ForceMode2D.Force);
            gripBody.AddForce(Vector2.up * (climbForce * pulse), ForceMode2D.Force);
        }

        // The softened radial springs preserve compression, but they also
        // transmit less of the gripping side's lift to the core. A modest
        // force-based core pull keeps the body climbing as one deformable mass
        // without reverting to a kinematic vertical velocity.
        float averagePulse = pulseSum / activeGripCount;
        if (averagePulse > 0f && climbCorePullForce > 0f)
        {
            _centerBody.AddForce(
                Vector2.up * (climbCorePullForce * averagePulse),
                ForceMode2D.Force);
        }
    }

    private void InsertClimbCandidate(int edgeIndex, float distanceSqr, int candidateCount)
    {
        for (int slot = 0; slot < candidateCount; slot++)
        {
            if (distanceSqr >= _climbNodeDistances[slot])
            {
                continue;
            }

            for (int shift = candidateCount - 1; shift > slot; shift--)
            {
                _climbNodeDistances[shift] = _climbNodeDistances[shift - 1];
                _climbNodeIndices[shift] = _climbNodeIndices[shift - 1];
            }

            _climbNodeDistances[slot] = distanceSqr;
            _climbNodeIndices[slot] = edgeIndex;
            return;
        }
    }

    private void SortClimbNodesBottomToTop(int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (_edgeBodies[_climbNodeIndices[i]].position.y <=
                    _edgeBodies[_climbNodeIndices[j]].position.y)
                {
                    continue;
                }

                int indexSwap = _climbNodeIndices[i];
                _climbNodeIndices[i] = _climbNodeIndices[j];
                _climbNodeIndices[j] = indexSwap;

                float distanceSwap = _climbNodeDistances[i];
                _climbNodeDistances[i] = _climbNodeDistances[j];
                _climbNodeDistances[j] = distanceSwap;
            }
        }
    }

    private static void ApplyVelocityToBody(Rigidbody2D body, float verticalSpeed, float horizontalBoost)
    {
        Vector2 velocity = body.velocity;
        velocity.y = Mathf.Max(velocity.y, verticalSpeed);
        if (Mathf.Abs(horizontalBoost) > 0.01f)
        {
            velocity.x = horizontalBoost > 0f
                ? Mathf.Max(velocity.x, horizontalBoost)
                : Mathf.Min(velocity.x, horizontalBoost);
        }

        body.velocity = velocity;
    }

    private void ApplyStickyWallMotion(Rigidbody2D body)
    {
        Vector2 velocity = body.velocity;
        velocity.y = Mathf.Max(velocity.y, _stickySlideSpeed);
        // Dampen rebound, while leaving enough control for the player to move
        // away from the wall using the normal slime movement input.
        velocity.x = Mathf.Lerp(velocity.x, 0f, 0.18f);
        body.velocity = velocity;
    }

    private void ApplyCornerSlideAssist()
    {
        if (_centerBody == null || cornerSlideForce <= 0f)
        {
            return;
        }

        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            Rigidbody2D edgeBody = _edgeBodies[i];
            CircleCollider2D edgeCollider = _edgeColliders[i];
            int contactCount = edgeCollider.GetContacts(_solidContactFilter, _cornerContacts);
            if (contactCount == 0)
            {
                continue;
            }

            float angle = (i / (float)_edgeBodies.Count) * Mathf.PI * 2f;
            Vector2 restOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Vector2 restError = (_centerBody.position + restOffset) - edgeBody.position;
            Vector2 desiredVelocity = _centerBody.velocity + restError * springFrequency;
            float stretchRatio = Vector2.Distance(edgeBody.position, _centerBody.position) /
                Mathf.Max(radius, 0.001f);

            // Do not disturb an already settled corner contact. The assist is
            // only active when the center is moving or the node is stretched.
            if (_centerBody.velocity.sqrMagnitude < 0.04f &&
                stretchRatio < cornerStretchThreshold)
            {
                continue;
            }

            for (int contactIndex = 0; contactIndex < contactCount; contactIndex++)
            {
                Vector2 normal = _cornerContacts[contactIndex].normal.normalized;
                float absX = Mathf.Abs(normal.x);
                float absY = Mathf.Abs(normal.y);
                if (absX < cornerNormalMinComponent || absY < cornerNormalMinComponent)
                {
                    continue;
                }

                Vector2 tangent = new Vector2(-normal.y, normal.x);
                float requestedSlip = Vector2.Dot(desiredVelocity - edgeBody.velocity, tangent);
                if (Mathf.Abs(requestedSlip) < 0.05f)
                {
                    continue;
                }

                float currentTangentSpeed = Mathf.Abs(Vector2.Dot(edgeBody.velocity, tangent));
                float stuckFactor = 1f - Mathf.Clamp01(currentTangentSpeed / cornerStuckSpeed);
                if (stuckFactor <= 0f)
                {
                    continue;
                }

                Vector2 slideDirection = tangent * Mathf.Sign(requestedSlip);
                edgeBody.AddForce(slideDirection * cornerSlideForce * stuckFactor, ForceMode2D.Force);
                break;
            }
        }
    }

    /// <summary>
    /// Pulls the lower leading shell into a low opening when the centre is
    /// pressing against its vertical lip. The assist is intentionally gated by
    /// grounded state and recent input so normal running and jumping are left
    /// entirely to the ordinary spring network.
    /// </summary>
    private void ApplySqueezeCrawlAssist()
    {
        if (!enableSqueezeCrawlAssist || crawlPullForce <= 0f ||
            _centerBody == null || _edgeBodies.Count == 0 || !IsGrounded ||
            Mathf.Abs(_lastMoveInput) < 0.1f ||
            Time.time - _lastMoveInputTime > crawlInputTimeout ||
            Mathf.Abs(_centerBody.velocity.x) >= crawlBlockedSpeed)
        {
            return;
        }

        int requestedCount = Mathf.Clamp(crawlLeadNodeCount, 2, 3);
        requestedCount = Mathf.Min(requestedCount, _edgeBodies.Count);

        int[] selectedIndices = { -1, -1, -1 };
        float[] selectedScores = { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
        float direction = Mathf.Sign(_lastMoveInput);
        float lowerHalfTop = _centerBody.position.y + radius * (crawlLowerHalfHeight - 0.5f);

        for (int edgeIndex = 0; edgeIndex < _edgeBodies.Count; edgeIndex++)
        {
            Vector2 edgePosition = _edgeBodies[edgeIndex].position;
            if ((edgePosition.y - _centerBody.position.y) > radius * crawlLowerHalfHeight)
            {
                continue;
            }

            float forwardDistance = direction * (edgePosition.x - _centerBody.position.x);
            if (forwardDistance < -edgeColliderRadius)
            {
                continue;
            }

            // Prefer the furthest forward node, then the one closest to the
            // lower-half centreline. This makes the nose enter first without
            // selecting a rear node that happens to be low enough.
            float score = -forwardDistance + Mathf.Abs(edgePosition.y - lowerHalfTop) * 0.2f;
            for (int slot = 0; slot < requestedCount; slot++)
            {
                if (score >= selectedScores[slot])
                {
                    continue;
                }

                for (int shift = requestedCount - 1; shift > slot; shift--)
                {
                    selectedScores[shift] = selectedScores[shift - 1];
                    selectedIndices[shift] = selectedIndices[shift - 1];
                }

                selectedScores[slot] = score;
                selectedIndices[slot] = edgeIndex;
                break;
            }
        }

        for (int slot = 0; slot < requestedCount; slot++)
        {
            int edgeIndex = selectedIndices[slot];
            if (edgeIndex < 0)
            {
                continue;
            }

            Rigidbody2D leadBody = _edgeBodies[edgeIndex];
            Vector2 leadVelocity = leadBody.velocity;
            leadVelocity.y = Mathf.Lerp(leadVelocity.y, _centerBody.velocity.y, 0.65f);
            leadBody.velocity = leadVelocity;
            leadBody.AddForce(Vector2.right * direction * crawlPullForce, ForceMode2D.Force);

            if (edgeIndex < _radialSprings.Count && _radialSprings[edgeIndex] != null)
            {
                _radialSprings[edgeIndex].dampingRatio =
                    _radialDamping[edgeIndex] * crawlRadialDampingMultiplier;
            }
        }
    }

    private void RestoreRadialSpringDamping()
    {
        int count = Mathf.Min(_radialSprings.Count, _radialDamping.Count);
        for (int i = 0; i < count; i++)
        {
            if (_radialSprings[i] != null)
            {
                _radialSprings[i].dampingRatio = _radialDamping[i];
            }
        }
    }

    private void BuildBody()
    {
        if (_built)
        {
            return;
        }

        _built = true;
        EnsureCenterBody();
        EnsureNoFrictionMaterial();
        ConfigureCenterCollider();
        ConfigureRigidbody(_centerBody, centerMass, centerLinearDrag);
        ApplyBodyLayerTo(gameObject);

        Transform edgeRoot = new GameObject("EdgeNodes").transform;
        edgeRoot.SetParent(transform, false);
        edgeRoot.localPosition = Vector3.zero;
        edgeRoot.localRotation = Quaternion.identity;
        edgeRoot.localScale = Vector3.one;
        ApplyBodyLayerTo(edgeRoot.gameObject);

        _edgeNodes.Clear();
        _edgeBodies.Clear();
        _edgeColliders.Clear();
        _radialSprings.Clear();
        _radialDamping.Clear();

        for (int i = 0; i < edgeNodeCount; i++)
        {
            float angle = (i / (float)edgeNodeCount) * Mathf.PI * 2f;
            Vector3 localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f);

            GameObject edgeObject = new GameObject(string.Format("Edge_{0:00}", i));
            ApplyBodyLayerTo(edgeObject);
            edgeObject.transform.SetParent(edgeRoot, false);
            edgeObject.transform.localPosition = localPosition;
            edgeObject.transform.localRotation = Quaternion.identity;
            edgeObject.transform.localScale = Vector3.one;

            Rigidbody2D edgeBody = edgeObject.AddComponent<Rigidbody2D>();
            CircleCollider2D edgeCollider = edgeObject.AddComponent<CircleCollider2D>();
            ConfigureRigidbody(edgeBody, edgeMass, edgeLinearDrag);
            edgeCollider.radius = edgeColliderRadius;
            edgeCollider.offset = Vector2.zero;
            edgeCollider.sharedMaterial = _noFrictionMaterial;
            // Small circular, frictionless contacts make the visible shell the
            // real support surface and slide over right-angle corners cleanly.
            edgeCollider.isTrigger = false;

            _edgeNodes.Add(edgeObject.transform);
            _edgeBodies.Add(edgeBody);
            _edgeColliders.Add(edgeCollider);
        }

        BuildSprings();
    }

    private void EnsureCenterBody()
    {
        _centerBody = GetComponent<Rigidbody2D>();
        if (_centerBody == null)
        {
            _centerBody = gameObject.AddComponent<Rigidbody2D>();
        }

        _centerCollider = GetComponent<CircleCollider2D>();
        if (_centerCollider == null)
        {
            _centerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
    }

    private void EnsureNoFrictionMaterial()
    {
        if (_noFrictionMaterial != null)
        {
            return;
        }

        _noFrictionMaterial = new PhysicsMaterial2D("JellyNoFriction")
        {
            friction = 0f,
            // Restitution is handled by the one-shot Super Bounce below.
            // Keeping the material non-bouncy prevents repeated physics-only
            // rebounds while the shell remains in contact with the floor.
            bounciness = 0f
        };
    }

    private void ConfigureCenterCollider()
    {
        _centerCollider.radius = centerColliderRadius;
        _centerCollider.offset = Vector2.zero;
        _centerCollider.sharedMaterial = _noFrictionMaterial;
    }

    private void ConfigureRigidbody(Rigidbody2D body, float mass, float linearDrag)
    {
        body.mass = mass;
        body.gravityScale = gravityScale;
        body.drag = linearDrag;
        body.angularDrag = angularDrag;
        body.interpolation = interpolation;
        body.collisionDetectionMode = collisionDetection;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.sleepMode = RigidbodySleepMode2D.StartAwake;
    }

    private void BuildSprings()
    {
        if (_edgeBodies.Count < 3) return;

        // 1. 辐射弹簧 (Center -> Edge)
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            SpringJoint2D radialSpring = _centerBody.gameObject.AddComponent<SpringJoint2D>();
            radialSpring.connectedBody = _edgeBodies[i];
            radialSpring.anchor = Vector2.zero;
            radialSpring.connectedAnchor = Vector2.zero;
            radialSpring.autoConfigureDistance = true;
            radialSpring.frequency = springFrequency;
            radialSpring.dampingRatio = springDampingRatio;
            radialSpring.enableCollision = false;
            _radialSprings.Add(radialSpring);
            _radialDamping.Add(radialSpring.dampingRatio);

            // This is only an emergency stretch limit. Normal gameplay may
            // stretch well beyond the original radius; the loose cap prevents
            // numerical explosions without making the body feel rigid.
            DistanceJoint2D maxStretch = _edgeBodies[i].gameObject.AddComponent<DistanceJoint2D>();
            maxStretch.connectedBody = _centerBody;
            maxStretch.anchor = Vector2.zero;
            maxStretch.connectedAnchor = Vector2.zero;
            maxStretch.autoConfigureDistance = false;
            maxStretch.maxDistanceOnly = true;
            maxStretch.distance = radius * maxStretchMultiplier;
            maxStretch.enableCollision = false;
        }

        // 2. 外壳弹簧 (Edge -> Next Edge)保留原有硬度乘数
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            int nextIndex = (i + 1) % _edgeBodies.Count;
            SpringJoint2D shellSpring = _edgeBodies[i].gameObject.AddComponent<SpringJoint2D>();
            shellSpring.connectedBody = _edgeBodies[nextIndex];
            shellSpring.anchor = Vector2.zero;
            shellSpring.connectedAnchor = Vector2.zero;
            shellSpring.autoConfigureDistance = true;
            shellSpring.frequency = springFrequency * shellFrequencyMultiplier;
            shellSpring.dampingRatio = Mathf.Clamp01(springDampingRatio * shellDampingMultiplier);
            shellSpring.enableCollision = false;
        }

        // 3. 内部交叉支撑弹簧 (Edge -> Opposite Edge)
        int edgeCount = _edgeBodies.Count;
        if (edgeCount % 2 != 0) return; // 节点数必须为偶数

        int halfCount = edgeCount / 2;
        for (int i = 0; i < halfCount; i++)
        {
            int oppositeIndex = (i + halfCount) % edgeCount;
            SpringJoint2D crossSpring = _edgeBodies[i].gameObject.AddComponent<SpringJoint2D>();
            crossSpring.connectedBody = _edgeBodies[oppositeIndex];
            crossSpring.anchor = Vector2.zero;
            crossSpring.connectedAnchor = Vector2.zero;
            crossSpring.autoConfigureDistance = true;
            crossSpring.frequency = springFrequency * crossFrequencyMultiplier;
            crossSpring.dampingRatio = Mathf.Clamp01(springDampingRatio * crossDampingMultiplier);
            crossSpring.enableCollision = false;
        }
    }

    private void ApplyBodyLayerTo(GameObject target)
    {
        int resolvedLayer = LayerMask.NameToLayer(bodyLayer);
        if (resolvedLayer < 0)
        {
            Debug.LogWarning($"JellySoftBody: Layer '{bodyLayer}' does not exist. Keeping '{target.name}' on layer {LayerMask.LayerToName(target.layer)}.", this);
            return;
        }

        target.layer = resolvedLayer;
    }

    private float GetSignedPolygonArea()
    {
        if (_edgeBodies == null || _edgeBodies.Count < 3) return 0f;

        float signedDoubleArea = 0f;
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            Vector2 current = _edgeBodies[i].position;
            Vector2 next = _edgeBodies[(i + 1) % _edgeBodies.Count].position;
            signedDoubleArea += current.x * next.y - next.x * current.y;
        }
        return signedDoubleArea * 0.5f;
    }

    private void ApplyGasPressure()
    {
        if (_edgeBodies == null || _edgeBodies.Count < 3 || Mathf.Approximately(targetArea, 0f)) return;

        float currentSignedArea = GetSignedPolygonArea();
        float currentArea = Mathf.Abs(currentSignedArea);
        float initialArea = Mathf.Abs(targetArea);

        if (currentArea <= Mathf.Epsilon) return;

        // 计算体积缩水/膨胀的比例
        float pressureDelta = (initialArea - currentArea) / initialArea;
        if (Mathf.Abs(pressureDelta) < pressureDeadZone)
        {
            return;
        }

        // A power curve keeps a small volume error gentle while still
        // producing a decisive restoring force after a severe stretch.
        float pressureResponse = Mathf.Sign(pressureDelta) *
            Mathf.Pow(Mathf.Abs(pressureDelta), pressureResponseExponent);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            Vector2 position = _edgeBodies[i].position;
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }

        float width = Mathf.Max(maxX - minX, 0.001f);
        float height = Mathf.Max(maxY - minY, 0.001f);
        float aspectRatio = width / height;
        float verticalCompression = 0f;
        if (pressureDelta > 0f && height < width * verticalCompressionAspect)
        {
            // The more pancake-like the body becomes, the more its pressure
            // is allowed to flow sideways instead of fighting the ceiling.
            verticalCompression = Mathf.Clamp01((aspectRatio - 1.5f) / 1.5f);
        }

        float pressureForce = pressureResponse * pressureMultiplier;
        pressureForce *= Mathf.Lerp(1f, compressedVerticalPressureScale, verticalCompression);
        float lateralScale = Mathf.Lerp(1f, lateralFlowMultiplier, verticalCompression);
        float verticalScale = Mathf.Lerp(1f, compressedVerticalPressureScale, verticalCompression);

        bool isCounterClockwise = currentSignedArea > 0f;

        // 给每个边缘节点施加推力/拉力
        for (int i = 0; i < _edgeBodies.Count; i++)
        {
            Rigidbody2D previousBody = _edgeBodies[(i - 1 + _edgeBodies.Count) % _edgeBodies.Count];
            Rigidbody2D currentBody = _edgeBodies[i];
            Rigidbody2D nextBody = _edgeBodies[(i + 1) % _edgeBodies.Count];

            Vector2 previous = previousBody.position;
            Vector2 current = currentBody.position;
            Vector2 next = nextBody.position;

            Vector2 previousEdge = current - previous;
            Vector2 nextEdge = next - current;

            Vector2 previousOutwardNormal;
            Vector2 nextOutwardNormal;

            if (isCounterClockwise)
            {
                previousOutwardNormal = new Vector2(previousEdge.y, -previousEdge.x);
                nextOutwardNormal = new Vector2(nextEdge.y, -nextEdge.x);
            }
            else
            {
                previousOutwardNormal = new Vector2(-previousEdge.y, previousEdge.x);
                nextOutwardNormal = new Vector2(-nextEdge.y, nextEdge.x);
            }

            Vector2 outwardNormal = (previousOutwardNormal.normalized + nextOutwardNormal.normalized).normalized;

            if (outwardNormal.sqrMagnitude > Mathf.Epsilon)
            {
                // During vertical compression, redirect the gas response into
                // the two horizontal sides. This preserves the area tendency
                // without making the top shell fight the low ceiling rigidly.
                Vector2 flowNormal = new Vector2(
                    outwardNormal.x * lateralScale,
                    outwardNormal.y * verticalScale);
                float flowMagnitude = flowNormal.magnitude;
                if (flowMagnitude > Mathf.Epsilon)
                {
                    currentBody.AddForce(
                        flowNormal / flowMagnitude * pressureForce * flowMagnitude,
                        ForceMode2D.Force);
                }
            }
        }
    }
}
