#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GrappleSettingsSO settings;

    [Header("Refs")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform cameraYaw;           // yaw pivot (same as in PlayerMovement)
    [SerializeField] private Camera viewCamera;             // assign gameplay camera (falls back to Camera.main)
    [SerializeField] private Transform playerOrigin;         // optional ray origin (chest). if null uses rb.position + up
    [SerializeField] private Collider playerCollider;        // assign player collider to ignore in LOS

    [Header("Input System")]
    [SerializeField] private InputActionReference grapple;   // Button (E)

    [Header("Optional: disable during grapple")]
    [SerializeField] private PlayerMovement movementToDisable;
    [SerializeField] private PalyerJump jumpToDisable;

    [Header("Targeting (camera view)")]
    [SerializeField] private bool requireOnScreen = true;
    [SerializeField] private bool requireInFrontOfCamera = true;
    [SerializeField, Range(0f, 0.45f)] private float viewportPadding = 0.06f;

    [Header("Line of Sight")]
    [SerializeField] private float losRadius = 0.08f;

    [Header("Debug / Visualize (Editor only)")]
    [SerializeField] private bool drawGrappleDebug = true;
    [SerializeField] private float debugLabelHeight = 0.35f;

    // state
    bool _isGrappling;
    GrapplePoint _target;
    Vector3 _startPos;
    Vector3 _targetPos;
    float _t;

    float _origDrag;
    bool _origUseGravity;

    float _cooldownRemaining;

    // reuse buffer (avoid alloc)
    readonly Collider[] _overlaps = new Collider[64];
    readonly RaycastHit[] _losHits = new RaycastHit[8];

    public bool IsGrappling => _isGrappling;
    public bool IsOnCooldown => _cooldownRemaining > 0f;

    void Awake()
    {
        if (viewCamera == null) viewCamera = Camera.main;
    }

    void OnEnable()
    {
        if (grapple != null) grapple.action.Enable();
    }

    void OnDisable()
    {
        if (grapple != null) grapple.action.Disable();
        ForceStopGrapple(applyExitImpulse: false);
    }

    void Update()
    {
        if (settings == null || rb == null || grapple == null) return;

        // cooldown tick
        if (_cooldownRemaining > 0f)
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);

        if (!_isGrappling)
        {
            if (grapple.action.WasPressedThisFrame())
            {
                if (IsOnCooldown) return;

                var best = FindBestTarget();
                if (best != null)
                    StartGrapple(best);
            }
        }
        else
        {
            // Optional: press again to cancel (no exit impulse)
            if (grapple.action.WasPressedThisFrame())
                ForceStopGrapple(applyExitImpulse: false);
        }
    }

    void FixedUpdate()
    {
        if (!_isGrappling) return;

        _t += Time.fixedDeltaTime;
        float dur = Mathf.Max(0.01f, settings.pullDuration);
        float t01 = Mathf.Clamp01(_t / dur);

        float curved = settings.pullCurve != null ? settings.pullCurve.Evaluate(t01) : t01;
        Vector3 newPos = Vector3.LerpUnclamped(_startPos, _targetPos, curved);

        rb.MovePosition(newPos);

        if (settings.stopWhenClose)
        {
            float dist = Vector3.Distance(rb.position, _targetPos);
            if (dist <= settings.stopDistance)
            {
                ForceStopGrapple(applyExitImpulse: true);
                return;
            }
        }

        if (t01 >= 1f)
            ForceStopGrapple(applyExitImpulse: true);
    }

    void StartGrapple(GrapplePoint target)
    {
        _isGrappling = true;
        _target = target;

        _t = 0f;
        _startPos = rb.position;
        _targetPos = target.AnchorPosition;

        // cooldown from config (explicit)
        float cd = settings.cooldownMode == GrappleCooldownMode.SameAsPullDuration
            ? settings.pullDuration
            : settings.cooldownSeconds;
        _cooldownRemaining = Mathf.Max(0.01f, cd);

        // disable other movement/jump while grappling
        if (movementToDisable != null) movementToDisable.enabled = false;
        if (jumpToDisable != null) jumpToDisable.enabled = false;

        // preserve rb params
        _origDrag = rb.linearDamping;
        _origUseGravity = rb.useGravity;

        // take control
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.linearDamping = settings.grappleDrag;
        if (settings.disableGravityWhileGrappling)
            rb.useGravity = false;
    }

    void ForceStopGrapple(bool applyExitImpulse)
    {
        if (!_isGrappling) return;

        _isGrappling = false;

        // restore rb params
        rb.linearDamping = _origDrag;
        if (settings != null && settings.disableGravityWhileGrappling)
            rb.useGravity = _origUseGravity;

        // re-enable systems
        if (movementToDisable != null) movementToDisable.enabled = true;
        if (jumpToDisable != null) jumpToDisable.enabled = true;

        if (applyExitImpulse && settings != null)
            ApplyExitImpulse();
    }

    void ApplyExitImpulse()
    {
        var v = rb.linearVelocity;
        v.y = Mathf.Max(0f, v.y);
        rb.linearVelocity = v;

        Vector3 fwd = settings.exitUsesCameraForward ? GetCameraForwardPlanar() : transform.forward;
        Vector3 impulse = Vector3.up * settings.exitUpImpulse + fwd * settings.exitForwardImpulse;

        rb.AddForce(impulse, ForceMode.Impulse);
    }

    GrapplePoint FindBestTarget()
    {
        Vector3 origin = GetRayOrigin();

        int count = Physics.OverlapSphereNonAlloc(
            origin,
            settings.queryRadius,
            _overlaps,
            settings.grappleMask,
            QueryTriggerInteraction.Collide
        );

        GrapplePoint best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < count; i++)
        {
            var col = _overlaps[i];
            if (col == null) continue;

            var gp = col.GetComponentInParent<GrapplePoint>();
            if (gp == null) continue;

            Vector3 p = gp.AnchorPosition;

            if (requireOnScreen && !IsInCameraView(p))
                continue;

            Vector3 to = p - origin;
            float dist = to.magnitude;
            if (dist <= 0.001f || dist > settings.maxDistance)
                continue;

            if (!HasLineOfSight(origin, p, dist))
                continue;

            float center = ScreenCenterScore01(p);

            // Higher is better (camera-centered + distance)
            float score =
                (center * 3.0f)
                - (dist / settings.maxDistance);

            if (score > bestScore)
            {
                bestScore = score;
                best = gp;
            }
        }

        return best;
    }

    bool IsInCameraView(Vector3 worldPoint)
    {
        if (viewCamera == null) return true;

        Vector3 vp = viewCamera.WorldToViewportPoint(worldPoint);

        if (requireInFrontOfCamera && vp.z <= 0f)
            return false;

        float min = viewportPadding;
        float max = 1f - viewportPadding;

        return vp.x >= min && vp.x <= max && vp.y >= min && vp.y <= max;
    }

    float ScreenCenterScore01(Vector3 worldPoint)
    {
        if (viewCamera == null) return 0f;

        Vector3 vp = viewCamera.WorldToViewportPoint(worldPoint);
        float dx = vp.x - 0.5f;
        float dy = vp.y - 0.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);

        return Mathf.Clamp01(1f - (d / 0.7071f));
    }

    bool HasLineOfSight(Vector3 origin, Vector3 point, float dist)
    {
        Vector3 dir = (point - origin).normalized;

        // Nudge forward so we don't immediately clip our own collider
        origin += dir * 0.05f;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            losRadius,
            dir,
            _losHits,
            dist,
            settings.obstructionMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            var h = _losHits[i];

            if (playerCollider != null && h.collider == playerCollider)
                continue;

            // Any other hit blocks
            return false;
        }

        return true;
    }

    Vector3 GetRayOrigin()
    {
        if (playerOrigin != null) return playerOrigin.position;
        return rb.position + Vector3.up * 1.2f;
    }

    Vector3 GetCameraForwardPlanar()
    {
        Vector3 fwd = (cameraYaw != null) ? cameraYaw.forward : transform.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude > 0.0001f ? fwd.normalized : transform.forward;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGrappleDebug || settings == null) return;

        Vector3 origin =
            rb != null ? GetRayOrigin() : (transform.position + Vector3.up * 1.2f);

        // Max grapple distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, settings.maxDistance);

        // Current grapple distance
        if (_isGrappling && _target != null)
        {
            Vector3 p = _target.AnchorPosition;
            float dist = Vector3.Distance(origin, p);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, p);
            Gizmos.DrawSphere(p, 0.12f);

            Handles.Label(p + Vector3.up * debugLabelHeight, $"Grapple: {dist:0.00}m");
        }

        // Cooldown label near player
        Handles.Label(origin + Vector3.up * 0.8f, IsOnCooldown ? $"CD: {_cooldownRemaining:0.00}s" : "CD: ready");
    }
#endif
}