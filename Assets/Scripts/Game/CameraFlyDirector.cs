using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFlyDirector : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera followCam;
    [SerializeField] private CinemachineCamera flyCam;

    [Header("Fly Cam Components (on flyCam)")]
    [SerializeField] private CinemachineSplineDolly dolly;
    [SerializeField] private CinemachineRotationComposer rotationComposer;

    [Header("Targets")]
    [SerializeField] private Transform lookAtTarget;

    [Header("Fly Settings")]
    [SerializeField] private float duration = 6f;
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("0..1 если PositionUnits=Normalized, иначе 'метры' если PositionUnits=Distance")]
    [SerializeField] private float startPos = 0f;
    [Tooltip("0..1 если PositionUnits=Normalized, иначе 'метры' если PositionUnits=Distance")]
    [SerializeField] private float endPos = 1f;

    [Header("Extra rotation while flying (optional)")]
    [SerializeField] private bool animateAimOffset = true;
    [SerializeField] private float orbitRadius = 2f;          
    [SerializeField] private float heightOffset = 1.5f;       
    [SerializeField] private float yawDegrees = 180f;        
    [SerializeField] private AnimationCurve yawCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Coroutine routine;
    private int followPriorityBefore;
    private int flyPriorityBefore;

    public void PlayFly()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        if (!followCam || !flyCam || !dolly)
            yield break;

        followPriorityBefore = followCam.Priority;
        flyPriorityBefore = flyCam.Priority;

        followCam.Priority = 0;
        flyCam.Priority = 20;

        if (lookAtTarget != null)
            flyCam.LookAt = lookAtTarget;

        dolly.CameraPosition = startPos;

        float t = 0f;
        while (t < duration)
        {
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));
            float cu = positionCurve.Evaluate(u);

            dolly.CameraPosition = Mathf.Lerp(startPos, endPos, cu);

            if (animateAimOffset && rotationComposer != null)
            {
                float yaw = yawDegrees * yawCurve.Evaluate(u);
                Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, heightOffset, orbitRadius);
                rotationComposer.TargetOffset = offset;
            }

            t += Time.deltaTime;
            yield return null;
        }

        flyCam.Priority = flyPriorityBefore;
        followCam.Priority = followPriorityBefore;

        routine = null;
    }
}
