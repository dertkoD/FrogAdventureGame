using UnityEngine;

[CreateAssetMenu(menuName = "Player/Jump Config", fileName = "PlayerJumpConfig")]
public class JumpSettingsSO : ScriptableObject
{
    [Header("Buffer/Coyote")]
    public float jumpBufferTime = 0.12f;
    public float coyoteTime     = 0.10f;

    [Header("Ground check")]
    public LayerMask groundMask = ~0;

    [Tooltip("Max slope angle from горизонтали. Smaller = stricter ground.")]
    public float maxSlopeAngle = 10f;

    [Tooltip("If upward velocity is above this, treat as airborne even if contacts still exist.")]
    public float leaveGroundVelocity = 0.05f;

    [Header("Variable height (hold window)")]
    [Tooltip("How long holding jump can increase jump from MIN to MAX (seconds). Typical: 0.12 - 0.25")]
    public float maxChargeTime = 0.15f;

    [Tooltip("Tap jump impulse (short hop).")]
    public float minUpImpulse = 6f;

    [Tooltip("Full jump impulse (when held long enough).")]
    public float maxUpImpulse = 10f;

    [Tooltip("Curve for ramping MIN->MAX while holding during maxChargeTime.")]
    public AnimationCurve charge01 = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
