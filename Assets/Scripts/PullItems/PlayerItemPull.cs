#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemPull : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ItemPullSettingsSO settings;

    [Header("Refs")]
    [SerializeField] private Rigidbody rb;                // player rigidbody (for position)
    [SerializeField] private Camera viewCamera;           // gameplay camera (fallback Camera.main)
    [SerializeField] private Transform playerCatchPoint;  // where items should fly to (hands/chest). if null: rb.position + up
    [SerializeField] private Collider playerCollider;     // used to ignore in LOS (optional)

    [Header("Input System")]
    [SerializeField] private InputActionReference pull;   // Button (e.g. E)

    [Header("Targeting (camera view)")]
    [SerializeField] private bool requireOnScreen = true;
    [SerializeField] private bool requireInFrontOfCamera = true;
    [SerializeField, Range(0f, 0.45f)] private float viewportPadding = 0.06f;

    [Header("Line of Sight")]
    [SerializeField] private float losRadius = 0.08f;

    [Header("Debug / Visualize (Editor only)")]
    [SerializeField] private bool drawDebug = true;
    [SerializeField] private float debugLabelHeight = 0.35f;

    // state
    bool _isPulling;
    PullableItem _item;
    Rigidbody _itemRb;

    Vector3 _startPos;
    float _t;

    float _origDrag;
    bool _origUseGravity;
    RigidbodyConstraints _origConstraints;
    bool _origKinematic;

    float _cooldownRemaining;

    // reuse buffers (avoid alloc)
    readonly Collider[] _overlaps = new Collider[64];
    readonly RaycastHit[] _losHits = new RaycastHit[8];

    // cache item colliders so LOS can ignore them
    Collider[] _itemCollidersCache;

    public bool IsPulling => _isPulling;
    public bool IsOnCooldown => _cooldownRemaining > 0f;

    void Awake()
    {
        if (viewCamera == null) viewCamera = Camera.main;
    }

    void OnEnable()
    {
        pull?.action.Enable();
    }

    void OnDisable()
    {
        pull?.action.Disable();
        ForceStopPull(arrived: false);
    }

    void Update()
    {
        if (settings == null || rb == null || pull == null) return;

        if (_cooldownRemaining > 0f)
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);

        if (!_isPulling)
        {
            if (pull.action.WasPressedThisFrame())
            {
                if (IsOnCooldown) return;

                var best = FindBestTarget();
                if (best != null)
                    StartPull(best);
            }
        }
        else
        {
            // press again to cancel
            if (pull.action.WasPressedThisFrame())
                ForceStopPull(arrived: false);
        }
    }

    void FixedUpdate()
    {
        if (!_isPulling) return;

        // target got destroyed / disabled mid pull
        if (_item == null || _itemRb == null)
        {
            ForceStopPull(arrived: false);
            return;
        }

        _t += Time.fixedDeltaTime;
        float dur = Mathf.Max(0.01f, settings.pullDuration);
        float t01 = Mathf.Clamp01(_t / dur);

        float curved = settings.pullCurve != null ? settings.pullCurve.Evaluate(t01) : t01;

        Vector3 targetPos = GetCatchPoint();
        Vector3 newPos = Vector3.LerpUnclamped(_startPos, targetPos, curved);

        _itemRb.MovePosition(newPos);

        if (settings.stopWhenClose)
        {
            float dist = Vector3.Distance(_itemRb.position, targetPos);
            if (dist <= settings.stopDistance)
            {
                ForceStopPull(arrived: true);
                return;
            }
        }

        if (t01 >= 1f)
            ForceStopPull(arrived: true);
    }

    void StartPull(PullableItem item)
    {
        if (item == null) return;

        var itemRb = item.ItemRigidbody;
        if (itemRb == null) return;

        _isPulling = true;
        _item = item;
        _itemRb = itemRb;

        _t = 0f;
        _startPos = _itemRb.position;

        // cache colliders for LOS ignore
        _itemCollidersCache = _item.GetComponentsInChildren<Collider>(includeInactive: false);

        // cooldown
        float cd = settings.cooldownMode == PullCooldownMode.SameAsPullDuration
            ? settings.pullDuration
            : settings.cooldownSeconds;
        _cooldownRemaining = Mathf.Max(0.01f, cd);

        // preserve rb params
        _origKinematic = _itemRb.isKinematic;
        _origDrag = _itemRb.linearDamping;
        _origUseGravity = _itemRb.useGravity;
        _origConstraints = _itemRb.constraints;

        // take control
        _itemRb.linearVelocity = Vector3.zero;
        _itemRb.angularVelocity = Vector3.zero;

        _itemRb.isKinematic = settings.makeKinematicWhilePulling;
        _itemRb.linearDamping = settings.pullDrag;

        if (settings.disableGravityWhilePulling)
            _itemRb.useGravity = false;

        if (settings.freezeRotationWhilePulling)
            _itemRb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void ForceStopPull(bool arrived)
    {
        if (!_isPulling) return;

        // snap first, while we still control the rb
        if (arrived && _itemRb != null)
        {
            Vector3 catchP = GetCatchPoint();

            // MovePosition is safer than setting .position in FixedUpdate context
            _itemRb.MovePosition(catchP);
            _itemRb.linearVelocity = Vector3.zero;
            _itemRb.angularVelocity = Vector3.zero;
        }

        // restore item rb params
        if (_itemRb != null)
        {
            _itemRb.isKinematic = _origKinematic;
            _itemRb.linearDamping = _origDrag;

            if (settings != null && settings.disableGravityWhilePulling)
                _itemRb.useGravity = _origUseGravity;

            if (settings != null && settings.freezeRotationWhilePulling)
                _itemRb.constraints = _origConstraints;
        }

        _isPulling = false;
        _item = null;
        _itemRb = null;
        _itemCollidersCache = null;
    }

    PullableItem FindBestTarget()
    {
        Vector3 origin = GetRayOrigin();

        int count = Physics.OverlapSphereNonAlloc(
            origin,
            settings.queryRadius,
            _overlaps,
            settings.pullMask,
            QueryTriggerInteraction.Collide
        );

        PullableItem best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < count; i++)
        {
            var col = _overlaps[i];
            if (col == null) continue;

            var item = col.GetComponentInParent<PullableItem>();
            if (item == null) continue;
            if (!item.CanBePulled) continue;
            if (item.ItemRigidbody == null) continue;

            Vector3 p = item.PullAnchorPosition;

            if (requireOnScreen && !IsInCameraView(p))
                continue;

            Vector3 to = p - origin;
            float dist = to.magnitude;
            if (dist <= 0.001f || dist > settings.maxDistance)
                continue;

            // ignore the candidate's own colliders in LOS
            var candidateColliders = item.GetComponentsInChildren<Collider>(false);

            if (!HasLineOfSight(origin, p, dist, candidateColliders))
                continue;

            float center = ScreenCenterScore01(p);

            float score =
                (center * 3.0f)
                - (dist / settings.maxDistance);

            if (score > bestScore)
            {
                bestScore = score;
                best = item;
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

    bool HasLineOfSight(Vector3 origin, Vector3 point, float dist, Collider[] candidateColliders)
    {
        Vector3 dir = (point - origin).normalized;

        // Nudge forward so we don't immediately clip something at the origin
        origin += dir * 0.05f;

        // dist should match nudged origin
        float maxDist = Mathf.Max(0f, dist - 0.05f);

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            losRadius,
            dir,
            _losHits,
            maxDist,
            settings.obstructionMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            var h = _losHits[i];
            if (h.collider == null) continue;

            if (playerCollider != null && h.collider == playerCollider)
                continue;

            // ignore the candidate item itself
            if (candidateColliders != null)
            {
                for (int c = 0; c < candidateColliders.Length; c++)
                {
                    if (candidateColliders[c] != null && h.collider == candidateColliders[c])
                        goto ContinueHits;
                }
            }

            // any other hit blocks
            return false;

        ContinueHits:
            continue;
        }

        return true;
    }

    Vector3 GetRayOrigin()
    {
        return rb.position + Vector3.up * settings.queryOriginUp;
    }

    Vector3 GetCatchPoint()
    {
        if (playerCatchPoint != null) return playerCatchPoint.position;
        return rb.position + Vector3.up * settings.catchPointUp;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawDebug || settings == null) return;

        Vector3 origin =
            rb != null
                ? (rb.position + Vector3.up * settings.queryOriginUp)
                : (transform.position + Vector3.up * settings.queryOriginUp);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, settings.maxDistance);

        if (_isPulling && _itemRb != null)
        {
            Vector3 p = _itemRb.position;
            Vector3 catchP = GetCatchPoint();

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p, catchP);
            Gizmos.DrawSphere(p, 0.12f);

            Handles.Label(p + Vector3.up * debugLabelHeight, $"Pulling: {Vector3.Distance(p, catchP):0.00}m");
        }

        Handles.Label(origin + Vector3.up * 0.8f, IsOnCooldown ? $"CD: {_cooldownRemaining:0.00}s" : "CD: ready");
    }
#endif
}
