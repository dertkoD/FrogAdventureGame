using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input System")]
    [SerializeField] private InputActionReference movement; // Vector2 (WASD)

    [Header("Assign in Inspector")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform cameraYaw;     // CameraRig (yaw pivot). Если null -> world-relative
    [SerializeField] private GroundChecker ground;    

    [Header("Speed")]
    [SerializeField] private float maxSpeed = 6.0f;
    [SerializeField] private float acceleration = 22.0f;
    [SerializeField] private float deceleration = 26.0f;

    [Header("Turning")]
    [SerializeField] private float turnSmoothTime = 0.08f;
    [SerializeField] private bool rotateToMove = true;

    [Header("Air Control")]
    [SerializeField, Range(0f, 1f)] private float airControl = 0.35f;

    [Header("Tuning")]
    [SerializeField] private float inputDeadzone = 0.08f;

    private Vector2 _moveInput;
    private float _yawVel;

    void OnEnable()
    {
        if (movement != null) movement.action.Enable();
    }

    void OnDisable()
    {
        if (movement != null) movement.action.Disable();
    }

    void Update()
    {
        if (movement == null) return;
        _moveInput = movement.action.ReadValue<Vector2>();
        if (_moveInput.sqrMagnitude < inputDeadzone * inputDeadzone)
            _moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        bool grounded = (ground != null) ? ground.IsGrounded : true;
        float control = grounded ? 1f : airControl;

        Vector3 wishDir = GetWishDirection(_moveInput);
        float inputMag = Mathf.Clamp01(_moveInput.magnitude);

        Vector3 v = rb.linearVelocity;
        Vector3 planar = new Vector3(v.x, 0f, v.z);

        Vector3 desired = wishDir * (maxSpeed * inputMag);

        float rate = (inputMag > 0f) ? acceleration : deceleration;
        rate *= control;

        planar = Vector3.MoveTowards(planar, desired, rate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(planar.x, v.y, planar.z);

        if (rotateToMove && wishDir.sqrMagnitude > 0.0001f)
        {
            float targetYaw = Mathf.Atan2(wishDir.x, wishDir.z) * Mathf.Rad2Deg;
            float smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _yawVel, turnSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0f, smoothYaw, 0f));
        }
    }

    private Vector3 GetWishDirection(Vector2 input)
    {
        if (input == Vector2.zero) return Vector3.zero;

        if (cameraYaw != null)
        {
            Vector3 f = cameraYaw.forward; f.y = 0f; f.Normalize();
            Vector3 r = cameraYaw.right;   r.y = 0f; r.Normalize();
            Vector3 dir = f * input.y + r * input.x;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        Vector3 d = new Vector3(input.x, 0f, input.y);
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.zero;
    }
}
