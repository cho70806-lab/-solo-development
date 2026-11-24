using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("부활 위치. 비워두면 이 오브젝트 위치")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color activatedColor = new Color(0.5f, 1f, 0.5f);

    private bool activated;

    // 컴포넌트를 처음 추가할 때 자동으로 Trigger로 설정
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;

        var respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        respawn.SetCheckpoint(pos);

        activated = true;
        if (sprite != null) sprite.color = activatedColor;
    }
}