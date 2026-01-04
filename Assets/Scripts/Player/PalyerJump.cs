using UnityEngine;
using UnityEngine.InputSystem;

public class PalyerJump : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private JumpSettingsSO s;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GroundChecker groundChecker;

    [Header("Input System")]
    [SerializeField] private InputActionReference jump;

    public bool IsGrounded { get; private set; }

    float _coyote;
    float _buffer;

    bool _isJumping;
    float _jumpStartTime;

    void OnEnable()
    {
        jump?.action.Enable();

        IsGrounded = groundChecker.IsGrounded;
        _coyote = IsGrounded ? s.coyoteTime : 0f;
        _buffer = 0f;
        _isJumping = false;
    }

    void OnDisable()
    {
        jump?.action.Disable();
    }

    void Update()
    {
        if (s == null || rb == null || groundChecker == null || jump == null)
            return;

        // --- Ground state ---
        IsGrounded = groundChecker.IsGrounded;

        if (_isJumping && rb.linearVelocity.y > s.leaveGroundVelocity)
            IsGrounded = false;

        _coyote = IsGrounded ? s.coyoteTime : Mathf.Max(0f, _coyote - Time.deltaTime);
        _buffer = Mathf.Max(0f, _buffer - Time.deltaTime);

        if (IsGrounded)
            _isJumping = false;

        // --- Input buffer ---
        if (jump.action.WasPressedThisFrame())
            _buffer = s.jumpBufferTime;

        // --- Start jump instantly ---
        if (_buffer > 0f && (IsGrounded || _coyote > 0f) && !_isJumping)
        {
            StartJump();
            _buffer = 0f;
            _coyote = 0f;
        }

        // --- Variable height (jump cut) ---
        if (jump.action.WasReleasedThisFrame())
            CutJump();
    }

    void StartJump()
    {
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.AddForce(Vector3.up * s.maxUpImpulse, ForceMode.Impulse);

        _isJumping = true;
        IsGrounded = false;
        _jumpStartTime = Time.time;
    }

    void CutJump()
    {
        if (!_isJumping)
            return;

        var v = rb.linearVelocity;
        if (v.y <= 0f)
            return;

        // Only allow cut shortly after takeoff
        if (Time.time - _jumpStartTime > s.maxChargeTime)
            return;

        // Reduce upward velocity to short-hop height
        float cutFactor = Mathf.Clamp01(
            s.minUpImpulse / Mathf.Max(0.01f, s.maxUpImpulse)
        );

        v.y *= cutFactor;
        rb.linearVelocity = v;
    }
}
