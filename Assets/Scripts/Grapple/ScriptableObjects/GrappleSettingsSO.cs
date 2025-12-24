using UnityEngine;

public enum GrappleCooldownMode
{
    SameAsPullDuration,
    FixedSeconds
}

[CreateAssetMenu(menuName = "Player/Grapple Config", fileName = "PlayerGrappleConfig")]
public class GrappleSettingsSO : ScriptableObject
{
    [Header("Targeting")]
    public float maxDistance = 18f;

    [Tooltip("Layers that contain GrapplePoint colliders.")]
    public LayerMask grappleMask = ~0;

    [Tooltip("What can block line-of-sight to the grapple point (usually Default + Environment).")]
    public LayerMask obstructionMask = ~0;

    [Tooltip("Sphere radius used to find nearby grapple points.")]
    public float queryRadius = 18f;

    [Header("Movement")]
    [Tooltip("How long the pull lasts (seconds). Also used for cooldown if mode is SameAsPullDuration.")]
    public float pullDuration = 0.35f;

    [Tooltip("Acceleration curve across the pull (0..1 time -> 0..1 progress).")]
    public AnimationCurve pullCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("If true, stop early when close enough to the anchor.")]
    public bool stopWhenClose = true;

    public float stopDistance = 1.1f;

    [Header("Exit impulse (inertia jump)")]
    public float exitUpImpulse = 4.5f;
    public float exitForwardImpulse = 6.0f;

    [Tooltip("Use camera forward for the forward part of exit impulse.")]
    public bool exitUsesCameraForward = true;

    [Header("While grappling")]
    public bool disableGravityWhileGrappling = true;
    public float grappleDrag = 0f;

    [Header("Cooldown")]
    public GrappleCooldownMode cooldownMode = GrappleCooldownMode.SameAsPullDuration;

    [Tooltip("Used only when Cooldown Mode = FixedSeconds")]
    public float cooldownSeconds = 0.35f;
}