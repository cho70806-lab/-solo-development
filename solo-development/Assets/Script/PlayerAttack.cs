using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlatformerPlayer))]
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private KeyCode attackKey = KeyCode.A;
    [Tooltip("공격 중에 키를 누르면 끝난 직후 바로 다음 공격 실행")]
    [SerializeField] private bool bufferInput = true;

    [Header("공격")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.1f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("공격 판정 (플레이어 발 기준, 오른쪽을 볼 때)")]
    [SerializeField] private Vector2 hitboxOffset = new Vector2(0.7f, 0.75f);
    [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.3f);
    [Tooltip("공격 애니메이션의 몇 번째 프레임부터 판정이 켜지는지 (0부터 시작)")]
    [SerializeField] private int hitStartFrame = 2;
    [SerializeField] private int hitEndFrame = 3;

    [Header("공격 중 이동")]
    [Range(0f, 1f)]
    [SerializeField] private float moveMultiplier = 0.3f;

    [Header("타격감")]
    [Tooltip("적을 맞췄을 때 화면이 잠깐 멈추는 시간 (0이면 끔)")]
    [SerializeField] private float hitStopDuration = 0.05f;

    private PlatformerPlayer player;
    private PlayerAnimator anim;
    private PlayerHealth health;

    private bool isAttacking;
    private bool hitActive;
    private bool queued;
    private float cooldownLeft;
    private bool hitStopping;

    // 한 번 휘두를 때 같은 적을 여러 번 때리지 않도록
    private readonly HashSet<EnemyHealth> hitTargets = new HashSet<EnemyHealth>();

    public bool IsAttacking => isAttacking;
    public float MoveMultiplier => isAttacking ? moveMultiplier : 1f;

    void Awake()
    {
        player = GetComponent<PlatformerPlayer>();
        anim = GetComponent<PlayerAnimator>();
        health = GetComponent<PlayerHealth>();
    }

    void OnEnable() { anim.FrameChanged += OnFrameChanged; }

    void OnDisable()
    {
        anim.FrameChanged -= OnFrameChanged;
        Cancel();
        if (hitStopping)
        {
            Time.timeScale = 1f;
            hitStopping = false;
        }
    }

    void Update()
    {
        if (health != null && health.IsDead) { Cancel(); return; }
        if (!player.enabled) return;

        if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;

        if (Input.GetKeyDown(attackKey))
        {
            if (!isAttacking && CanAttack()) Begin();
            else if (isAttacking && bufferInput) queued = true;
        }

        if (!isAttacking) return;

        if (hitActive) DoHit();

        // 공격 애니메이션이 끝나면 종료
        if (anim.Current == PlayerAnimator.State.Attack && anim.IsFinished)
        {
            End();
            if (queued && CanAttack())
            {
                queued = false;
                Begin();
            }
            queued = false;
        }
    }

    private bool CanAttack()
    {
        return cooldownLeft <= 0f && !player.IsDashing && !player.IsKnockedBack;
    }

    private void Begin()
    {
        isAttacking = true;
        hitActive = false;
        hitTargets.Clear();
        anim.Play(PlayerAnimator.State.Attack, true);
    }

    private void End()
    {
        isAttacking = false;
        hitActive = false;
        cooldownLeft = cooldown;
    }

    // 대쉬나 피격으로 공격이 끊길 때 PlatformerPlayer가 호출
    public void Cancel()
    {
        isAttacking = false;
        hitActive = false;
        queued = false;
    }

    private void OnFrameChanged(int frame)
    {
        if (!isAttacking || anim.Current != PlayerAnimator.State.Attack) return;
        hitActive = frame >= hitStartFrame && frame <= hitEndFrame;
    }

    private void DoHit()
    {
        float dir = Mathf.Sign(transform.localScale.x);
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * dir, hitboxOffset.y);

        Collider2D[] cols = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, enemyLayers);
        bool landed = false;

        foreach (var c in cols)
        {
            var enemy = c.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !hitTargets.Add(enemy)) continue;

            enemy.TakeDamage(damage, transform.position);
            landed = true;
        }

        if (landed && hitStopDuration > 0f && !hitStopping)
            StartCoroutine(HitStop());
    }

    private IEnumerator HitStop()
    {
        hitStopping = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
        hitStopping = false;
    }

    // 씬 뷰에서 공격 범위 확인용
    void OnDrawGizmosSelected()
    {
        float dir = Application.isPlaying ? Mathf.Sign(transform.localScale.x) : 1f;
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * dir, hitboxOffset.y);
        Gizmos.color = hitActive ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(center, hitboxSize);
    }
}