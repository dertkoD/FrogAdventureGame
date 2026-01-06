using UnityEngine;

public class PlatformTrigger : MonoBehaviour
{
    public BreakAndFallPlatform platform;
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        platform.TriggerFall();
    }
}
