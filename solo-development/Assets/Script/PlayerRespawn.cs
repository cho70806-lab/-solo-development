using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("리스폰 설정")]
    [Tooltip("비워두면 게임 시작 시 플레이어 위치가 최초 리스폰 지점")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private float respawnDelay = 1.5f;

    [Header("화면 페이드 (선택)")]
    [Tooltip("검은 Image가 붙은 CanvasGroup. 비워두면 페이드 없음")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeTime = 0.4f;

    private PlayerHealth health;
    private Rigidbody2D rb;
    private Vector3 respawnPosition;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
        respawnPosition = startPoint != null ? startPoint.position : transform.position;

        if (fadeGroup != null) fadeGroup.alpha = 0f;
    }

    void OnEnable()
    {
        health.onDied.AddListener(OnPlayerDied);
    }

    void OnDisable()
    {
        health.onDied.RemoveListener(OnPlayerDied);
    }

    public void SetCheckpoint(Vector3 position)
    {
        respawnPosition = position;
    }

    private void OnPlayerDied()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // 죽은 동안 물리 정지 (시체가 계속 떨어지거나 함정에 반복 반응하지 않도록)
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        bool useFade = fadeGroup != null;
        yield return new WaitForSeconds(useFade ? Mathf.Max(0f, respawnDelay - fadeTime) : respawnDelay);

        if (useFade) yield return Fade(0f, 1f);

        transform.position = respawnPosition;
        rb.simulated = true;
        health.Revive();

        if (useFade) yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        fadeGroup.alpha = to;
    }
}