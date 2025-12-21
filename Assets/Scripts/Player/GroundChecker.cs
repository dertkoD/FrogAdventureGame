using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Collider playerCollider;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float extraDistance = 0.08f;   
    [SerializeField] private float skin = 0.01f;            
    [SerializeField] private float maxSlopeAngle = 60f;     
    [SerializeField] private bool drawDebug = false;

    public bool IsGrounded { get; private set; }

    private RaycastHit _hit;

    void FixedUpdate()
    {
        IsGrounded = CheckGroundedCast();
        if (drawDebug)
            Debug.DrawLine(GetCastOrigin(), GetCastOrigin() + Vector3.down * GetCastDistance(), IsGrounded ? Color.green : Color.red, Time.fixedDeltaTime);
    }

    private bool CheckGroundedCast()
    {
        if (playerCollider == null) return false;

        Vector3 origin = GetCastOrigin();
        float radius = GetCastRadius();
        float distance = GetCastDistance();

        bool hit = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _hit,
            distance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hit) return false;

        float minDot = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
        float dot = Vector3.Dot(_hit.normal, Vector3.up);
        return dot >= minDot;
    }

    private Vector3 GetCastOrigin()
    {
        Bounds b = playerCollider.bounds;
        float r = GetCastRadius();
        return new Vector3(b.center.x, b.min.y + r + skin, b.center.z);
    }

    private float GetCastDistance()
    {
        return extraDistance + skin;
    }

    private float GetCastRadius()
    {
        Bounds b = playerCollider.bounds;
        float r = Mathf.Min(b.extents.x, b.extents.z);
        r = Mathf.Max(0.01f, r - skin);
        return r;
    }
}
