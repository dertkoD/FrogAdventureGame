using UnityEngine;
using UnityEngine.InputSystem;

public class PalyerJump : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private JumpSettingsSO s;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GroundChecker groundChecker;

    [Header("Input System")]
    [SerializeField] private InputActionReference jump; // Button (Space)
    [SerializeField] private InputActionReference move; // Value Vector2 (WASD)

    [Header("Movement direction source")]
    [SerializeField] private Transform movementReference;

    [Header("Tuning")]
    [SerializeField] private float moveDeadzone = 0.08f;

    public bool IsGrounded { get; private set; }

    private float _coyote;
    private float _buffer;

    private bool _isJumping;
    private bool _charging;
    private float _chargeStartTime;

    void OnEnable()
    {
        if (jump != null) jump.action.Enable();
        if (move != null) move.action.Enable();

        IsGrounded = groundChecker != null && groundChecker.IsGrounded;
        _coyote = IsGrounded ? s.coyoteTime : 0f;
        _buffer = 0f;

        _isJumping = false;
        _charging = false;
    }

    void OnDisable()
    {
        if (jump != null) jump.action.Disable();
        if (move != null) move.action.Disable();
    }

    void Update()
    {
        if (s == null || rb == null || groundChecker == null || jump == null || move == null)
            return;

        IsGrounded = groundChecker.IsGrounded;

        if (_isJumping && rb.linearVelocity.y > s.leaveGroundVelocity)
            IsGrounded = false;

        _coyote = IsGrounded ? s.coyoteTime : Mathf.Max(0f, _coyote - Time.deltaTime);
        _buffer = Mathf.Max(0f, _buffer - Time.deltaTime);

        if (IsGrounded && _isJumping) _isJumping = false;

        if (jump.action.WasPressedThisFrame())
        {
            _buffer = s.jumpBufferTime;
        }

        if (!_charging && _buffer > 0f && (IsGrounded || _coyote > 0f))
        {
            _charging = true;
            _chargeStartTime = Time.time;
            _buffer = 0f;
        }

        if (_charging && jump.action.WasReleasedThisFrame())
        {
            _charging = false;

            float t = Mathf.Clamp01((Time.time - _chargeStartTime) / Mathf.Max(0.01f, s.maxChargeTime));
            float curved = (s.charge01 != null) ? s.charge01.Evaluate(t) : t;

            float up = Mathf.Lerp(s.minUpImpulse, s.maxUpImpulse, curved);
            float fwd = Mathf.Lerp(s.minForwardImpulse, s.maxForwardImpulse, curved);

            DoJump(up, fwd);

            _coyote = 0f;
        }

        if (_charging && !(IsGrounded || _coyote > 0f))
        {
            _charging = false;
        }
    }

    private void DoJump(float upImpulse, float forwardImpulse)
    {
        var v = rb.linearVelocity;
        v.y = 0f;

        Vector3 dir = GetMoveDirection();
        Vector3 planarImpulse = (dir.sqrMagnitude > 0.0001f) ? (dir * forwardImpulse) : Vector3.zero;

        rb.linearVelocity = new Vector3(v.x, 0f, v.z);
        rb.AddForce(Vector3.up * upImpulse + planarImpulse, ForceMode.Impulse);

        _isJumping = true;
        IsGrounded = false;
    }

    private Vector3 GetMoveDirection()
    {
        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude < moveDeadzone * moveDeadzone)
            return Vector3.zero;

        if (movementReference != null)
        {
            Vector3 f = movementReference.forward; f.y = 0f; f.Normalize();
            Vector3 r = movementReference.right;   r.y = 0f; r.Normalize();
            Vector3 d = f * input.y + r * input.x;
            return (d.sqrMagnitude > 0.0001f) ? d.normalized : Vector3.zero;
        }

        Vector3 world = new Vector3(input.x, 0f, input.y);
        return (world.sqrMagnitude > 0.0001f) ? world.normalized : Vector3.zero;
    }
}
