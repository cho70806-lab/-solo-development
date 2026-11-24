using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlatformerPlayer))]
public class PlayerHealth : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private int maxHp = 5;

    [Header("피격 후 무적")]
    [SerializeField] private float invincibleDuration = 1f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("넉백")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackUpForce = 4f;

    [Header("이벤트 (UI, 사운드 연결용)")]
    public UnityEvent<int, int> onHpChanged; // (현재 HP, 최대 HP)
    public UnityEvent onDamaged;
    public UnityEvent onDied;

    private int currentHp;
    private bool isInvincible;
    private bool isDead;

    private PlatformerPlayer player;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsDead => isDead;

    void Awake()
    {
        player = GetComponent<PlatformerPlayer>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        currentHp = maxHp;
    }

    void Start()
    {
        onHpChanged?.Invoke(currentHp, maxHp);
    }

    /// <summary>
    /// 적이나 함정에서 호출합니다.
    /// hitSourcePosition: 공격이 온 위치 (넉백 방향 계산용)
    /// </summary>
    public void TakeDamage(int damage, Vector2 hitSourcePosition)
    {
        if (isDead) return;

        // 대쉬 중이거나 피격 무적 중이면 데미지 무시
        if (player.IsDashing || isInvincible) return;

        currentHp = Mathf.Max(currentHp - damage, 0);
        onHpChanged?.Invoke(currentHp, maxHp);
        onDamaged?.Invoke();

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        // 넉백: 공격이 온 반대 방향으로 튕겨나감
        float dir = transform.position.x >= hitSourcePosition.x ? 1f : -1f;
        player.ApplyKnockback(new Vector2(dir * knockbackForce, knockbackUpForce));

        StartCoroutine(InvincibleRoutine());
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        onHpChanged?.Invoke(currentHp, maxHp);
    }

    // 낙사처럼 무적이나 대쉬와 상관없이 즉사시킬 때
    public void Kill()
    {
        if (isDead) return;
        currentHp = 0;
        onHpChanged?.Invoke(currentHp, maxHp);
        onDamaged?.Invoke();
        Die();
    }

    // 리스폰 시 상태 복구
    public void Revive()
    {
        isDead = false;
        isInvincible = false;
        StopAllCoroutines();

        currentHp = maxHp;
        onHpChanged?.Invoke(currentHp, maxHp);

        player.enabled = true;
        player.ResetState();

        StartCoroutine(InvincibleRoutine()); // 부활 직후 잠깐 무적
    }

    private IEnumerator InvincibleRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibleDuration)
        {
            if (sprite != null) sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (sprite != null) sprite.enabled = true;
        isInvincible = false;
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();
        if (sprite != null) sprite.enabled = true;

        rb.linearVelocity = Vector2.zero;
        player.enabled = false; // 조작 불가
        onDied?.Invoke();

        Debug.Log("플레이어 사망");
    }
}