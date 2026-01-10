using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShotPerEnter = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var tracker = other.GetComponentInParent<RespawnPointTracker>();
        if (!tracker) return;

        if (oneShotPerEnter)
        {
            tracker.RespawnToLastSafe();
        }
        else
        {
            tracker.RespawnToLastSafe();
        }
    }
}
