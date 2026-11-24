using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.Kill();
    }
}