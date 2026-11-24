using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private Transform target;

    [Header("따라가기")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

    [Header("맵 경계 (선택)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 5f);

    private Vector3 velocity;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // 플레이어 이동이 끝난 뒤에 카메라가 움직여야 떨림이 없음
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z // 카메라 Z값은 유지
        );

        if (useBounds && cam != null && cam.orthographic)
        {
            // 카메라 화면 절반 크기만큼 안쪽으로 제한해야 화면 밖이 안 보임
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x + halfW, maxBounds.x - halfW);
            targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y + halfH, maxBounds.y - halfH);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    // 에디터에서 맵 경계 확인용
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}
