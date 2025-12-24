using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealingFly : MonoBehaviour
{
    [SerializeField] private int healAmount = 10;
    [SerializeField] private string playerTag = "Player";

    void Reset()
    {
        // Ensure trigger
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.Heal(healAmount);
        Destroy(gameObject);
    }
}
