using UnityEngine;

public enum PullCooldownMode
{
    SameAsPullDuration,
    ExplicitSeconds
}

[CreateAssetMenu(menuName = "Player/Item Pull Settings", fileName = "ItemPullSettings")]
public class ItemPullSettingsSO : ScriptableObject
{
    [Header("Query")]
    public float queryRadius = 8f;
    public float maxDistance = 12f;
    public LayerMask pullMask = ~0;

    [Tooltip("World geometry that can block pulling (walls etc.)")]
    public LayerMask obstructionMask = ~0;

    [Header("Pull Motion")]
    public float pullDuration = 0.25f;
    public AnimationCurve pullCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool stopWhenClose = true;
    public float stopDistance = 0.35f;

    [Header("RB control")]
    public float pullDrag = 0f;
    public bool disableGravityWhilePulling = true;
    public bool makeKinematicWhilePulling = true;
    public bool freezeRotationWhilePulling = true;

    [Header("Cooldown")]
    public PullCooldownMode cooldownMode = PullCooldownMode.SameAsPullDuration;
    public float cooldownSeconds = 0.25f;

    [Header("Heights")]
    [Tooltip("Ray/origin used for query and LOS (player position + up).")]
    public float queryOriginUp = 1.2f;

    [Tooltip("Catch point if playerCatchPoint is not assigned (player position + up).")]
    public float catchPointUp = 1.2f;
}