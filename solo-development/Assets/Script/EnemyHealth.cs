using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private int maxHp = 3;

    [Header("피격 반응")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpForce = 3f;
    [Tooltip("맞은 뒤 행동불능 시간 (나중에 적 AI가 IsStunned를 확인해서 멈춥니다)")]
    [SerializeField] private float stunTime = 0.25f;
    [SerializeField] private float flashTime = 0.1f;
    [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f);

    [Header("사망")]
    [SerializeField] private float deathFadeTime = 0.3f;

    public UnityEvent onDamaged;
    public UnityEvent onDied;

    private int currentHp;
    private float stunLeft;
    private bool isDead;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Color originalColor = Color.white;

    public bool IsDead => isDead;
    public bool IsStunned => stunLeft > 0f;
    public int CurrentHp => currentHp;

    void Awake()
    {
        currentHp = maxHp;
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
    }

    void Update()
    {
        if (stunLeft > 0f) stunLeft -= Time.deltaTime;
    }

    public void TakeDamage(int damage, Vector2 hitSourcePosition)
    {
        if (isDead) return;

        currentHp -= damage;
        onDamaged?.Invoke();

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        stunLeft = stunTime;

        if (rb != null)
        {
            float dir = transform.position.x >= hitSourcePosition.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * knockbackForce, knockbackUpForce);
        }

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (sprite != null) sprite.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        if (sprite != null) sprite.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();
        onDied?.Invoke();

        // 죽은 적이 플레이어에게 데미지를 주지 않도록 정리
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        var dmg = GetComponent<EnemyDamage>();
        if (dmg != null) dmg.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float t = 0f;
        while (t < deathFadeTime)
        {
            t += Time.deltaTime;
            if (sprite != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(originalColor.a, 0f, t / deathFadeTime);
                sprite.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
