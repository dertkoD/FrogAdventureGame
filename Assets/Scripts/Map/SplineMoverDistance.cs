using System;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineMoverDistance : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex = 0;
    [SerializeField] private bool loop = false;

    [Header("Speed")]
    [SerializeField] private float speed = 2f; // м/с

    [Header("Rotation")]
    [SerializeField] private bool alignToSpline = true;
    [SerializeField] private Vector3 forwardAxis = Vector3.forward;
    [SerializeField] private Vector3 upAxis = Vector3.up;

    [Header("Sampling (meters<->t)")]
    [SerializeField] private int samples = 500;

    private Rigidbody rb;

    private float[] tSamples;
    private float[] cumLen;
    private float totalLength;

    private float currentT;   // 0..1
    private float currentLen; // 0..totalLength

    private bool fleeing;      // игрок внутри
    private bool movingToStop; // едем до targetLen после выхода
    private float targetLen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        if (!splineContainer || splineContainer.Splines.Count == 0)
        {
            Debug.LogError($"{name}: SplineContainer не назначен или пустой.");
            enabled = false;
            return;
        }

        BuildArcLengthTable();

        currentT = FindNearestT(transform.position, 30);
        currentLen = LengthAtT(currentT);
        ApplyPose(currentT);
    }

    // Вызывайте при входе игрока в триггер
    public void StartFlee()
    {
        fleeing = true;
        movingToStop = false;
    }

    // Вызывайте при выходе игрока из триггера
    public void StopFleeAndMoveExtra(float extraDistanceMeters)
    {
        fleeing = false;

        // если сплайн не loop и мы уже в конце — просто остановимся
        if (!loop && currentLen >= totalLength - 0.0001f)
        {
            movingToStop = false;
            return;
        }

        float desired = currentLen + Mathf.Max(0f, extraDistanceMeters);
        targetLen = loop ? Mod(desired, totalLength) : Mathf.Clamp(desired, 0f, totalLength);
        movingToStop = true;
    }

    private void FixedUpdate()
    {
        if (!fleeing && !movingToStop) return;

        float step = speed * Time.fixedDeltaTime;

        if (fleeing)
        {
            // пока игрок внутри — едем постоянно вперёд
            currentLen = AdvanceLength(currentLen, step, loop);
        }
        else if (movingToStop)
        {
            // игрок вышел — едем до цели и останавливаемся
            currentLen = MoveTowardsLength(currentLen, targetLen, step, loop);

            if (!loop)
            {
                if (Mathf.Abs(currentLen - targetLen) <= 0.0005f)
                    movingToStop = false;
            }
            else
            {
                // для loop: если попали в цель в пределах шага — стоп
                if (LoopReached(currentLen, targetLen, step))
                    movingToStop = false;
            }
        }

        currentT = TAtLength(currentLen);
        ApplyPose(currentT);
    }

    private void ApplyPose(float t)
    {
        Spline spline = splineContainer.Splines[splineIndex];

        Vector3 worldPos = splineContainer.transform.TransformPoint((Vector3)spline.EvaluatePosition(t));
        rb.MovePosition(worldPos);

        if (!alignToSpline) return;

        Vector3 tangent = splineContainer.transform.TransformDirection((Vector3)spline.EvaluateTangent(t));
        if (tangent.sqrMagnitude < 1e-6f) return;

        Vector3 fwd = tangent.normalized;
        Vector3 up = upAxis.normalized;
        if (Mathf.Abs(Vector3.Dot(fwd, up)) > 0.99f) up = Vector3.up;

        Quaternion look = Quaternion.LookRotation(fwd, up);

        // компенсация оси модели, если она смотрит не по Z
        Quaternion axisFix = Quaternion.FromToRotation(forwardAxis.normalized, Vector3.forward);
        Quaternion finalRot = look * Quaternion.Inverse(axisFix);

        rb.MoveRotation(finalRot);
    }

    private void BuildArcLengthTable()
    {
        samples = Mathf.Max(10, samples);

        Spline spline = splineContainer.Splines[splineIndex];

        tSamples = new float[samples + 1];
        cumLen = new float[samples + 1];

        tSamples[0] = 0f;
        cumLen[0] = 0f;

        float3 prev = spline.EvaluatePosition(0f);
        float acc = 0f;

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            float3 p = spline.EvaluatePosition(t);
            acc += math.distance(prev, p);

            tSamples[i] = t;
            cumLen[i] = acc;

            prev = p;
        }

        totalLength = acc;
        if (totalLength < 1e-6f)
        {
            Debug.LogError($"{name}: Длина сплайна слишком мала.");
            enabled = false;
        }
    }

    private float TAtLength(float length)
    {
        float len = loop ? Mod(length, totalLength) : Mathf.Clamp(length, 0f, totalLength);

        int idx = Array.BinarySearch(cumLen, len);
        if (idx >= 0) return tSamples[idx];

        idx = ~idx;
        if (idx <= 0) return 0f;
        if (idx >= cumLen.Length) return 1f;

        float l0 = cumLen[idx - 1];
        float l1 = cumLen[idx];
        float t0 = tSamples[idx - 1];
        float t1 = tSamples[idx];

        float u = (len - l0) / Mathf.Max(1e-6f, (l1 - l0));
        return Mathf.Lerp(t0, t1, u);
    }

    private float LengthAtT(float t)
    {
        t = Mathf.Clamp01(t);
        float ft = t * samples;
        int i = Mathf.Clamp(Mathf.FloorToInt(ft), 0, samples - 1);
        float u = ft - i;
        return Mathf.Lerp(cumLen[i], cumLen[i + 1], u);
    }

    private float FindNearestT(Vector3 worldPos, int coarseSteps)
    {
        Spline spline = splineContainer.Splines[splineIndex];
        float3 localPos = (float3)splineContainer.transform.InverseTransformPoint(worldPos);

        coarseSteps = Mathf.Max(10, coarseSteps);

        float bestT = 0f;
        float bestD = float.MaxValue;

        for (int i = 0; i <= coarseSteps; i++)
        {
            float t = (float)i / coarseSteps;
            float3 p = spline.EvaluatePosition(t);
            float d = math.lengthsq(p - localPos);
            if (d < bestD) { bestD = d; bestT = t; }
        }

        float span = 1f / coarseSteps;
        float tMin = Mathf.Clamp01(bestT - span);
        float tMax = Mathf.Clamp01(bestT + span);

        for (int k = 0; k < 10; k++)
        {
            float t1 = Mathf.Lerp(tMin, tMax, 1f / 3f);
            float t2 = Mathf.Lerp(tMin, tMax, 2f / 3f);

            float d1 = math.lengthsq(spline.EvaluatePosition(t1) - localPos);
            float d2 = math.lengthsq(spline.EvaluatePosition(t2) - localPos);

            if (d1 < d2) tMax = t2;
            else tMin = t1;
        }

        return (tMin + tMax) * 0.5f;
    }

    private static float Mod(float a, float m)
    {
        if (m <= 0f) return 0f;
        float r = a % m;
        return r < 0f ? r + m : r;
    }

    private float AdvanceLength(float current, float delta, bool loopMode)
    {
        if (loopMode) return Mod(current + delta, totalLength);
        return Mathf.Clamp(current + delta, 0f, totalLength);
    }

    private float MoveTowardsLength(float current, float target, float maxDelta, bool loopMode)
    {
        if (!loopMode) return Mathf.MoveTowards(current, target, maxDelta);

        // Для loop: двигаемся кратчайшим путём по окружности длины totalLength
        float a = Mod(current, totalLength);
        float b = Mod(target, totalLength);

        float forward = Mod(b - a, totalLength);
        float backward = Mod(a - b, totalLength);

        if (forward <= backward)
        {
            float step = Mathf.Min(maxDelta, forward);
            return Mod(a + step, totalLength);
        }
        else
        {
            float step = Mathf.Min(maxDelta, backward);
            return Mod(a - step, totalLength);
        }
    }

    private bool LoopReached(float current, float target, float step)
    {
        float a = Mod(current, totalLength);
        float b = Mod(target, totalLength);
        float d = Mathf.Min(Mod(b - a, totalLength), Mod(a - b, totalLength));
        return d <= Mathf.Max(0.001f, step * 0.5f);
    }
}
