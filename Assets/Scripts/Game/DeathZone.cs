using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Header("Damage")]
    [SerializeField] private int damageOnEnter = 10;
    [SerializeField] private bool oneShotPerEnter = true;

    bool _triggered;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (oneShotPerEnter && _triggered) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.Damage(damageOnEnter);
        }

        var tracker = other.GetComponentInParent<RespawnPointTracker>();
        if (tracker != null)
        {
            tracker.RespawnToLastSafe();
        }

        _triggered = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!oneShotPerEnter) return;
        if (!other.CompareTag(playerTag)) return;

        // Reset so next entry can damage again
        _triggered = false;
    }
}
