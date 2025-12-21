using UnityEngine;
using UnityEngine.InputSystem;

public class PalyerJump : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private JumpSettingsSO s;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GroundChecker groundChecker;

    [Header("Input System")]
    [SerializeField] private InputActionReference jump; // Button action (Space)

    [Header("Optional: jump forward by camera yaw")]
    [SerializeField] private Transform forwardReference; // CameraRig yaw pivot (optional)

    public bool IsGrounded { get; private set; }

    private float _coyote;
    private float _buffer;

    private bool _isJumping;
    private bool _charging;
    private float _chargeStartTime;

    private bool _jumpReleased;

    void OnEnable()
    {
        if (jump != null) jump.action.Enable();

        IsGrounded = groundChecker != null && groundChecker.IsGrounded;
        _coyote = IsGrounded ? s.coyoteTime : 0f;
        _buffer = 0f;

        _isJumping = false;
        _charging = false;
        _jumpReleased = false;
    }

    void OnDisable()
    {
        if (jump != null) jump.action.Disable();
    }

    void Update()
    {
        if (s == null || rb == null || groundChecker == null || jump == null)
            return;

        // Read grounded from GroundChecker (it updates in FixedUpdate)
        IsGrounded = groundChecker.IsGrounded;

        // Like in your 2D: if we just jumped and moving up, don't count grounded
        if (_isJumping && rb.linearVelocity.y > s.leaveGroundVelocity)
            IsGrounded = false;

        _coyote = IsGrounded ? s.coyoteTime : Mathf.Max(0f, _coyote - Time.deltaTime);
        _buffer = Mathf.Max(0f, _buffer - Time.deltaTime);

        if (IsGrounded && _isJumping) _isJumping = false;

        // Input System: started = press, canceled = release
        if (jump.action.WasPressedThisFrame())
            RequestJump();

        if (jump.action.WasReleasedThisFrame())
            NotifyJumpReleased();

        // Mode logic
        if (s.useChargedJump)
        {
            // Start charging when we have buffered press and can jump
            if (_buffer > 0f && (IsGrounded || _coyote > 0f) && !_charging)
            {
                _charging = true;
                _chargeStartTime = Time.time;
            }

            // Release -> jump
            if (_charging && _jumpReleased)
            {
                _jumpReleased = false;
                _charging = false;

                float t = Mathf.Clamp01((Time.time - _chargeStartTime) / Mathf.Max(0.01f, s.maxChargeTime));
                float curved = (s.charge01 != null) ? s.charge01.Evaluate(t) : t;

                float up = Mathf.Lerp(s.minUpImpulse, s.maxUpImpulse, curved);
                float fwd = Mathf.Lerp(s.minForwardImpulse, s.maxForwardImpulse, curved);

                DoJump(up, fwd);

                _buffer = 0f;
                _coyote = 0f;
            }
        }
        else
        {
            // Fixed jump
            if (_buffer > 0f && (IsGrounded || _coyote > 0f))
            {
                DoJump(s.fixedUpImpulse, s.fixedForwardImpulse);
                _buffer = 0f;
                _coyote = 0f;
            }
        }
    }

    private void RequestJump()
    {
        _buffer = s.jumpBufferTime;
    }

    private void NotifyJumpReleased()
    {
        _jumpReleased = true;
    }

    private void DoJump(float upImpulse, float forwardImpulse)
    {
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        Vector3 fwd = GetForwardDir();
        Vector3 impulse = Vector3.up * upImpulse + fwd * forwardImpulse;

        rb.AddForce(impulse, ForceMode.Impulse);

        _isJumping = true;
        IsGrounded = false;
    }

    private Vector3 GetForwardDir()
    {
        if (forwardReference != null)
        {
            var d = forwardReference.forward;
            d.y = 0f;
            return (d.sqrMagnitude > 0.0001f) ? d.normalized : transform.forward;
        }

        Vector3 planar = rb.linearVelocity;
        planar.y = 0f;
        if (planar.sqrMagnitude > 0.01f) return planar.normalized;

        var f = transform.forward; f.y = 0f;
        return (f.sqrMagnitude > 0.0001f) ? f.normalized : Vector3.forward;
    }
}
