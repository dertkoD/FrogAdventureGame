using UnityEngine;

public class GrapplePoint : MonoBehaviour
{
    [Tooltip("Optional override for the exact point to grapple to (e.g. a child Transform).")]
    public Transform anchor;

    public Vector3 AnchorPosition => anchor != null ? anchor.position : transform.position;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(AnchorPosition, 0.12f);
    }
}