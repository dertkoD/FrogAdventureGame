using UnityEngine;

[CreateAssetMenu(menuName = "Player/Jump Config", fileName = "PlayerJumpConfig")]
public class JumpSettingsSO : ScriptableObject
{
    [Header("Buffer/Coyote")]
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.1f;

    [Header("Ground check")]
    public LayerMask groundMask = ~0;
    [Tooltip("Максимальный угол поверхности от горизонта. 10° ≈ как в Jumper2D (80..100 нормаль)")]
    public float maxSlopeAngle = 10f;

    [Tooltip("Upward velocity that immediately marks jump as airborne before contacts separate")]
    public float leaveGroundVelocity = 0.05f;

    [Header("Charged jump")]
    public float maxChargeTime = 0.8f;

    public float minUpImpulse = 4f;
    public float maxUpImpulse = 10f;

    public float minForwardImpulse = 3f;
    public float maxForwardImpulse = 12f;

    public AnimationCurve charge01 = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
