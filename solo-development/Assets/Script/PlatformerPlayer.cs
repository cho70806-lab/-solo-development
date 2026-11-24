using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlatformerPlayer : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("점프")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private int maxJumpCount = 2; // 2 = 더블 점프

    [Header("대쉬")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private bool resetDashOnGround = false;

    [Tooltip("대쉬 중 통과할 레이어 (적, 물체 등. 바닥/벽은 선택하지 마세요)")]
    [SerializeField] private LayerMask dashPassLayers;

    [Header("바닥 판정")]
    [Tooltip("접촉면이 이 값 이상으로 위를 향하면 바닥으로 인정 (1 = 완전 평평, 0.7 = 약 45도 경사까지)")]
    [SerializeField] private float groundNormalThreshold = 0.7f;

    private Rigidbody2D rb;
    private Collider2D col;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    private float moveInput;
    private bool isGrounded;
    private bool jumpRequested;
    private int jumpCount;

    // 대쉬 상태
    private bool isDashing;
    private bool dashRequested;
    private float dashTimeLeft;
    private float dashCooldownLeft;
    private float dashDirection;
    private float defaultGravityScale;
    private int playerLayer;
    private DashAfterImage afterImage;
    private PlayerAttack attack;

    private float facing = 1f;

    // 넉백 상태
    private bool isKnockedBack;
    private float knockbackTimeLeft;
    private const float KnockbackDuration = 0.2f;

    public bool IsDashing => isDashing;
    public bool IsGrounded => isGrounded;
    public bool IsKnockedBack => isKnockedBack;
    public Vector2 Velocity => rb.velocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        defaultGravityScale = rb.gravityScale;
        playerLayer = gameObject.layer;
        afterImage = GetComponent<DashAfterImage>(); // 추가 (없어도 오류 없음)
        attack = GetComponent<PlayerAttack>(); // 추가
    }

    void Update()
    {
        moveInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) moveInput -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) moveInput += 1f;

        if (isDashing) return;

        // 기존: if (moveInput != 0f)
        if (moveInput != 0f && (attack == null || !attack.IsAttacking))
        {
            facing = Mathf.Sign(moveInput);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            transform.localScale = scale;
        }

        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            jumpRequested = true;
        }

        bool shiftDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        if (shiftDown && dashCooldownLeft <= 0f)
        {
            dashRequested = true;
        }
    }

    void FixedUpdate()
    {
        if (dashCooldownLeft > 0f)
        {
            dashCooldownLeft -= Time.fixedDeltaTime;
        }

        // 바닥 판정
        bool wasGrounded = isGrounded;
        isGrounded = CheckGrounded();

        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            jumpCount = 0;
            if (resetDashOnGround) dashCooldownLeft = 0f;
        }
        else if (wasGrounded && !isGrounded && jumpCount == 0 && !isDashing && !jumpRequested)
        {
            // 점프 없이 발판에서 떨어짐 → 첫 점프를 소비한 것으로 처리
            jumpCount = 1;
        }

        // 넉백 중에는 조작 불가
        if (isKnockedBack)
        {
            knockbackTimeLeft -= Time.fixedDeltaTime;
            if (knockbackTimeLeft <= 0f) isKnockedBack = false;
            return;
        }

        // 대쉬 시작
        if (dashRequested)
            // 대쉬 시작
            if (dashRequested)
            {
                dashRequested = false;
                StartDash();
            }

        // 대쉬 진행
        if (isDashing)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

            if (dashTimeLeft <= 0f)
            {
                EndDash();
            }
            return;
        }

        
        // 일반 이동 (공격 중에는 느려짐)
        float mult = attack != null ? attack.MoveMultiplier : 1f;
        rb.velocity = new Vector2(moveInput * moveSpeed * mult, rb.velocity.y);

        // 점프
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
            jumpRequested = false;
        }
    }

    // 현재 닿아 있는 모든 접촉면 중, 위를 향한 면이 있으면 바닥
    private bool CheckGrounded()
    {
        int count = col.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y >= groundNormalThreshold)
            {
                return true;
            }
        }
        return false;
    }

    // 리스폰 시 모든 상태 초기화
    public void ResetState()
    {
        if (isDashing) EndDash();

        isDashing = false;
        dashRequested = false;
        jumpRequested = false;
        isKnockedBack = false;
        knockbackTimeLeft = 0f;
        jumpCount = 0;
        dashCooldownLeft = 0f;

        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = Vector2.zero;
    }
    public void ApplyKnockback(Vector2 force)
    {
        if (attack != null) attack.Cancel();
        // 진행 중이던 대쉬/점프 요청 취소
        if (isDashing) EndDash();
        dashRequested = false;
        jumpRequested = false;

        isKnockedBack = true;
        knockbackTimeLeft = KnockbackDuration;
        rb.linearVelocity = force;
    }

    private void StartDash()
    {
        if (attack != null) attack.Cancel();
        isDashing = true;
        dashTimeLeft = dashDuration;
        dashCooldownLeft = dashCooldown;
        dashDirection = facing;
        jumpRequested = false;

        rb.gravityScale = 0f;
        SetPassThrough(true); // 충돌 무시 시작
        if (afterImage != null) afterImage.StartEffect();
    }

    private void EndDash()
    {
        isDashing = false;
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, 0f);
        SetPassThrough(false); // 충돌 복구
        if (afterImage != null) afterImage.StopEffect();
    }

    // 선택한 레이어와 플레이어 사이의 충돌을 켜고 끔
    private void SetPassThrough(bool ignore)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((dashPassLayers.value & (1 << i)) != 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, i, ignore);
            }
        }
    }

    // 대쉬 중에 비활성화되거나 사망해도 충돌이 꺼진 채로 남지 않도록 복구
    void OnDisable()
    {
        if (isDashing) SetPassThrough(false);
    }
}