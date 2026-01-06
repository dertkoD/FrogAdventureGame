using System.Collections;
using UnityEngine;

public class BreakAndFallPlatform : MonoBehaviour
{
    public Rigidbody rb;
    public float breakDelay = 0.15f;
    public float destroyAfterFallSeconds = 2f;

    bool triggered;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void TriggerFall()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        yield return new WaitForSeconds(breakDelay);
        rb.isKinematic = false;
        rb.useGravity = true;
        Destroy(gameObject, destroyAfterFallSeconds);
    }
}
