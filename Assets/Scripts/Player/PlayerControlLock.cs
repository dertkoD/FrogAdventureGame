using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    [Header("Auto-find if empty")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PalyerJump jump;
    [SerializeField] private Rigidbody rb;

    [Header("Freeze options")]
    [Tooltip("Обнулять горизонтальную скорость при блокировке.")]
    [SerializeField] private bool zeroPlanarVelocity = true;

    [Tooltip("Обнулять вертикальную скорость при блокировке (чтобы не падал).")]
    [SerializeField] private bool zeroVerticalVelocity = true;

    [Tooltip("Заморозить физику полностью (rb.isKinematic) на время блокировки.")]
    [SerializeField] private bool makeKinematicWhileLocked = false;

    [Tooltip("Смещение вверх при разлочке (если боитесь застреваний).")]
    [SerializeField] private float unlockUpNudge = 0f;

    private bool _locked;
    private bool _wasKinematic;

    public bool IsLocked => _locked;

    private void Awake()
    {
        if (!movement) movement = GetComponentInChildren<PlayerMovement>(true);
        if (!jump) jump = GetComponentInChildren<PalyerJump>(true);
        if (!rb) rb = GetComponentInChildren<Rigidbody>(true);
    }

    public void SetLocked(bool locked)
    {
        if (_locked == locked) return;
        _locked = locked;

        if (locked)
        {
            if (movement) movement.enabled = false;
            if (jump) jump.enabled = false;

            if (rb)
            {
                _wasKinematic = rb.isKinematic;

                if (zeroPlanarVelocity || zeroVerticalVelocity)
                {
                    Vector3 v = rb.linearVelocity;
                    if (zeroPlanarVelocity) { v.x = 0f; v.z = 0f; }
                    if (zeroVerticalVelocity) v.y = 0f;
                    rb.linearVelocity = v;
                    rb.angularVelocity = Vector3.zero;
                }

                if (makeKinematicWhileLocked)
                    rb.isKinematic = true;
            }
        }
        else
        {
            if (rb && makeKinematicWhileLocked)
                rb.isKinematic = _wasKinematic;

            if (unlockUpNudge != 0f)
                transform.position += Vector3.up * unlockUpNudge;

            if (movement) movement.enabled = true;
            if (jump) jump.enabled = true;
        }
    }
}
