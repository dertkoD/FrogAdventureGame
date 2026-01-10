using UnityEngine;

public class RespawnPointTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GroundChecker groundChecker;

    [Header("Save rules")]
    [Tooltip("Как часто обновлять точку (сек). 0 = каждый FixedUpdate.")]
    [SerializeField] private float saveInterval = 0.05f;

    [Tooltip("Доп. смещение вверх при телепорте, чтобы не застрять в земле.")]
    [SerializeField] private float respawnUpOffset = 0.2f;

    public Vector3 LastSafePosition { get; private set; }
    public Quaternion LastSafeRotation { get; private set; }

    private Rigidbody _rb;
    private CharacterController _cc;
    private float _timer;

    private void Reset()
    {
        groundChecker = GetComponentInChildren<GroundChecker>();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cc = GetComponent<CharacterController>();

        if (!groundChecker)
            groundChecker = GetComponentInChildren<GroundChecker>();

        LastSafePosition = transform.position;
        LastSafeRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        if (!groundChecker) return;

        if (saveInterval > 0f)
        {
            _timer += Time.fixedDeltaTime;
            if (_timer < saveInterval) return;
            _timer = 0f;
        }

        if (groundChecker.IsGrounded)
        {
            LastSafePosition = transform.position;
            LastSafeRotation = transform.rotation;
        }
    }

    public void RespawnToLastSafe()
    {
        Vector3 targetPos = LastSafePosition + Vector3.up * respawnUpOffset;

        if (_rb)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = targetPos;
            _rb.rotation = LastSafeRotation;
            return;
        }

        if (_cc)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(targetPos, LastSafeRotation);
            _cc.enabled = true;
            return;
        }

        transform.SetPositionAndRotation(targetPos, LastSafeRotation);
    }
}
