using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    void OnCollisionStay2D(Collision2D collision)
    {
        var health = collision.gameObject.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage, transform.position);
        }
    }

    // 적에 Is Trigger를 켠 경우를 위한 대응
    void OnTriggerStay2D(Collider2D other)
    {
        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage, transform.position);
        }
    }
}