using UnityEngine;

public class OneShotDamageTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private int damage = 10;

    bool _used;
    

    void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        if (!other.CompareTag(playerTag)) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.Damage(damage);
        _used = true;

        GetComponent<Collider>().enabled = false;
    }
}
