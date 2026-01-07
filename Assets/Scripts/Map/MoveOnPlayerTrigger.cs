using UnityEngine;

public class MoveOnPlayerTrigger : MonoBehaviour
{
    [SerializeField] private SplineMoverDistance mover;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float distanceMeters = 3f;

    private int playerInsideCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!mover) return;

        playerInsideCount++;
        if (playerInsideCount == 1)
            mover.StartFlee();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!mover) return;

        playerInsideCount = Mathf.Max(0, playerInsideCount - 1);
        if (playerInsideCount == 0)
            mover.StopFleeAndMoveExtra(distanceMeters);
    }
}
