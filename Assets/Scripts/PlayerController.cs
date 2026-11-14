using System.Collections.Generic;
using UnityEngine;

public enum Form { Normal, Wing, Slime }

public class PlayerController : MonoBehaviour
{
    [Header("核心组件分配")]
    public GameObject trueSlimePrefab; // 【新增】拖入你做好的真史莱姆预制体
    private JellySoftBody activeJelly; // 当前激活的真史莱姆脚本
    public CameraFollow cameraFollow;  // 【新增】拖入摄像机跟随脚本，用于切换镜头目标

    [Header("移动设置")]
    public float moveSpeed = 6f;
    private float horizontalInput;

    [Header("形态系统")]
    public List<Form> activeForms = new List<Form>() { Form.Normal, Form.Wing, Form.Slime };
    public Form currentForm = Form.Normal;
    private int formIndex = 0;

    [Header("白球/蓝翅跳跃参数")]
    public float normalJumpForce = 3.5f;   
    private bool isGrounded;
    private bool _wasGrounded;
    [SerializeField] private bool flightUnlocked;
    [SerializeField] private bool slimeFormUnlocked;

    [Header("翅膀耐久系统")]
    public float maxStamina = 100f;        
    public float currentStamina = 100f;
    public float staminaDrainPerTap = 20f; 
    public float initialFlapForce = 4.5f;
    [SerializeField] private float glideFallSpeed = -2f;
    public float minFlapForce = 2f;        
    public float staminaRecoveryRate = 40f;

    [Header("飞行赋能区")]
    public bool isInInfiniteFlightZone;
    [SerializeField, Min(0.1f)] private float flappyFlapVelocity = 4f;

    public bool IsClimbingTree => currentForm == Form.Normal && activeTreeClimber != null;
    public int TreeAttachmentSide => treeAttachSide;

    private CatTreeClimber activeTreeClimber;
    private float treeGravityScale;
    private int treeAttachSide = 1;
    private float treeClimbSpeed;
    private float treeKickHorizontal;
    private float treeKickVertical;
    private float treeDetachUntil;
    private float treeFlipUntil;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private CapsuleCollider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 如果你之前改成了 GetComponentInChildren，请保持
        col = GetComponent<CapsuleCollider2D>();

        // LateUpdate cameras need an interpolated physics pose. Without this,
        // render frames between FixedUpdate ticks can show visible stepping.
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
        }

        // The scene may still have an old slime assigned in the inspector.
        // Normal/Wing always start by following this controller's ball.
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
        }

        if (!IsFormUnlocked(currentForm))
        {
            currentForm = Form.Normal;
        }

        ApplyFormAttributes();
    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Tab 键切换形态
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchToNextUnlockedForm();
        }

        // ================= 状态分流控制 =================

        if (currentForm == Form.Normal && activeTreeClimber != null)
        {
            UpdateTreeClimb();
            return;
        }

        if (currentForm == Form.Slime)
        {
            // 史莱姆状态下，把输入交给真史莱姆的代码去处理！
            if (activeJelly != null)
            {
                activeJelly.Move(horizontalInput);
                if (Input.GetButtonDown("Jump") && activeJelly.IsGrounded)
                {
                    activeJelly.Jump(5.5f); // 调佣真软体的跳跃
                }
            }
        }
        else
        {
            // 白球和蓝翅膀的控制逻辑
            // 每次离开地面都开启一段全新的飞行，衰弱效果只在本段飞行内累积。
            if (_wasGrounded && !isGrounded)
            {
                currentStamina = maxStamina;
            }
            _wasGrounded = isGrounded;

            if (isGrounded)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoveryRate * Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && currentForm != Form.Wing && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, normalJumpForce);
            }
            else if (flightUnlocked && currentForm == Form.Wing && Input.GetButtonDown("Jump"))
            {
                if (isInInfiniteFlightZone)
                {
                    FlapInfiniteFlight();
                }
                else
                {
                    TryFlapWing();
                }
            }
        }
    }

    private GUIStyle staminaLabel;

    private void OnGUI()
    {
        if (currentForm != Form.Wing) return;
        if (staminaLabel == null)
        {
            staminaLabel = new GUIStyle(GUI.skin.label);
            staminaLabel.fontSize = 16;
            staminaLabel.fontStyle = FontStyle.Bold;
            staminaLabel.normal.textColor = Color.white;
        }

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        float uiScale = Mathf.Clamp(Screen.height / 720f, 0.75f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
        float ratio = isInInfiniteFlightZone ? 1f :
            (maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f);
        GUI.color = new Color(0.08f, 0.10f, 0.15f, 0.92f);
        GUI.DrawTexture(new Rect(20f, 20f, 264f, 72f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        string value = isInInfiniteFlightZone ? "无限" :
            Mathf.CeilToInt(Mathf.Max(0f, currentStamina)) + " / " + Mathf.CeilToInt(Mathf.Max(0f, maxStamina));
        GUI.Label(new Rect(32f, 26f, 240f, 26f), "飞行耐久  " + value, staminaLabel);
        GUI.color = new Color(0.22f, 0.25f, 0.30f);
        GUI.DrawTexture(new Rect(32f, 58f, 240f, 20f), Texture2D.whiteTexture);
        GUI.color = ratio <= 0.2f ? new Color(1f, 0.35f, 0.28f) : new Color(0.45f, 0.86f, 0.95f);
        GUI.DrawTexture(new Rect(34f, 60f, 236f * ratio, 16f), Texture2D.whiteTexture);
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    void FixedUpdate()
    {
        // 只有不是史莱姆的时候，旧的刚体才移动
        if (activeTreeClimber != null && currentForm == Form.Normal)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else if (currentForm != Form.Slime)
        {
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }

        // Wing form behaves like a parachute while descending.
        if (currentForm == Form.Wing && rb.velocity.y < glideFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, glideFallSpeed);
        }
    }

    private void UpdateTreeClimb()
    {
        if (activeTreeClimber == null || currentForm != Form.Normal)
        {
            return;
        }

        float horizontalTreeInput = Input.GetAxisRaw("Horizontal");
        // Pressing into the trunk means deliberately moving to its other
        // side. The position is placed outside the solid collider, so this
        // is a clean flip rather than a collision-prone pass-through.
        if (horizontalTreeInput * -treeAttachSide > 0.1f && Time.time >= treeFlipUntil)
        {
            FlipTreeSide();
        }

        float verticalInput = Input.GetAxisRaw("Vertical");
        rb.velocity = new Vector2(0f, verticalInput * treeClimbSpeed);

        if (Input.GetButtonDown("Jump"))
        {
            WallKickFromTree();
        }
    }

    public void TryAttachToTree(CatTreeClimber climber)
    {
        if (climber == null || currentForm != Form.Normal)
        {
            return;
        }

        if (Time.time < treeDetachUntil)
        {
            return;
        }

        if (activeTreeClimber != climber)
        {
            activeTreeClimber = climber;
            treeGravityScale = rb.gravityScale;
            treeClimbSpeed = climber.ClimbSpeed;
            treeKickHorizontal = climber.WallKickHorizontal;
            treeKickVertical = climber.WallKickVertical;
            treeAttachSide = climber.GetAttachmentSide(rb.position.x, treeAttachSide);
            SnapToTreeSide();
        }

        rb.gravityScale = 0f;
    }

    public void UnlockFlight()
    {
        flightUnlocked = true;
        Debug.Log("PlayerController: flight unlocked.");
    }

    public void DetachFromTree(CatTreeClimber climber)
    {
        if (activeTreeClimber != climber)
        {
            return;
        }

        activeTreeClimber = null;
        treeDetachUntil = Time.time + 0.08f;
        rb.gravityScale = treeGravityScale;
        rb.velocity = Vector2.zero;
    }

    private void WallKickFromTree()
    {
        CatTreeClimber climber = activeTreeClimber;
        DetachFromTree(climber);
        rb.velocity = new Vector2(treeAttachSide * treeKickHorizontal, treeKickVertical);
    }

    private void FlipTreeSide()
    {
        if (activeTreeClimber == null)
        {
            return;
        }

        treeAttachSide = -treeAttachSide;
        SnapToTreeSide();
        treeFlipUntil = Time.time + 0.08f;
    }

    private void SnapToTreeSide()
    {
        if (activeTreeClimber == null)
        {
            return;
        }

        float targetX = activeTreeClimber.GetAttachmentX(col, treeAttachSide);
        rb.position = new Vector2(targetX, rb.position.y);
        rb.velocity = new Vector2(0f, rb.velocity.y);
        Physics2D.SyncTransforms();
    }

    void TryFlapWing()
    {
        if (currentStamina <= 0) return;
        float staminaPercent = Mathf.Clamp01(currentStamina / maxStamina);
        float currentFlapForce = Mathf.Lerp(minFlapForce, initialFlapForce, staminaPercent);
        rb.velocity = new Vector2(rb.velocity.x, currentFlapForce);
        currentStamina = Mathf.Max(0, currentStamina - staminaDrainPerTap);
    }

    private void FlapInfiniteFlight()
    {
        // The zone deliberately uses a fixed impulse, independent of stamina,
        // so its rhythm feels like Flappy Bird instead of a weakening glide.
        if (rb.velocity.y < 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
        }

        rb.velocity = new Vector2(rb.velocity.x, flappyFlapVelocity);
        currentStamina = maxStamina;
    }

    private bool IsFormUnlocked(Form form)
    {
        switch (form)
        {
            case Form.Normal: return true;
            case Form.Wing: return flightUnlocked;
            case Form.Slime: return slimeFormUnlocked;
            default: return false;
        }
    }

    private void SwitchToNextUnlockedForm()
    {
        if (activeForms == null || activeForms.Count == 0)
        {
            return;
        }

        int currentIndex = activeForms.IndexOf(currentForm);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        for (int step = 1; step <= activeForms.Count; step++)
        {
            int candidateIndex = (currentIndex + step) % activeForms.Count;
            Form candidate = activeForms[candidateIndex];
            if (!IsFormUnlocked(candidate))
            {
                continue;
            }

            formIndex = candidateIndex;
            SwitchForm(candidate);
            return;
        }
    }

    public void UnlockSlimeForm()
    {
        slimeFormUnlocked = true;
        Debug.Log("PlayerController: slime form unlocked.");
    }

    void SwitchForm(Form targetForm)
    {
        if (currentForm == targetForm || !IsFormUnlocked(targetForm)) return;

        if (activeTreeClimber != null)
        {
            DetachFromTree(activeTreeClimber);
        }

        // 核心逻辑：动量与位置的交接传递
        if (targetForm == Form.Slime)
        {
            // 1. 从白球切史莱姆：生成真史莱姆，把白球的速度传给它
            Vector2 savedVel = rb.velocity;
            
            if (activeJelly == null)
            {
                GameObject slimeObj = Instantiate(trueSlimePrefab, transform.position, Quaternion.identity);
                activeJelly = slimeObj.GetComponent<JellySoftBody>();
                // 预制体默认是 inactive；先激活，确保 Awake 已建立中心和边缘刚体。
                activeJelly.gameObject.SetActive(true);
            }
            else
            {
                activeJelly.gameObject.SetActive(true);
                activeJelly.transform.position = transform.position;
            }
            
            activeJelly.SetCenterVelocity(savedVel);
            
            // 隐藏旧的白球躯壳
            sr.enabled = false;
            rb.simulated = false; // 彻底关闭旧物理，防止干扰

            // 镜头转移
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(activeJelly.transform);
            }
        }
        else if (currentForm == Form.Slime && targetForm != Form.Slime)
        {
            // 2. 从史莱姆切回白球/蓝翅膀：读取真史莱姆的位置和速度传给白球
            if (activeJelly != null)
            {
                transform.position = activeJelly.transform.position;
                rb.velocity = activeJelly.GetCenterVelocity();
                activeJelly.gameObject.SetActive(false); // 隐藏真软体
            }

            // 唤醒旧的白球躯壳
            sr.enabled = true;
            rb.simulated = true;

            // 镜头转移
            if (cameraFollow != null) cameraFollow.SetTarget(transform);
        }

        currentForm = targetForm;
        ApplyFormAttributes(); // 刷新颜色等
    }

    void ApplyFormAttributes()
    {
        if (currentForm == Form.Slime) return; // 史莱姆外观由新脚本管，直接跳过

        transform.localScale = Vector3.one;
        col.offset = Vector2.zero;
        
        switch (currentForm)
        {
            case Form.Normal:
                sr.color = Color.white;
                rb.gravityScale = 3f;
                col.size = new Vector2(1f, 1f);
                break;
            case Form.Wing:
                sr.color = Color.white;
                rb.gravityScale = 1f;                  
                col.size = new Vector2(1f, 1f);
                break;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contacts[0].normal.y > 0.6f) isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }
}
