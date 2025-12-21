using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMouseLookRig : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private Transform player;       // Player transform
    [SerializeField] private Transform yawPivot;     // обычно CameraRig
    [SerializeField] private Transform pitchPivot;   // обычно CameraTarget (child of yawPivot)

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference look; // Vector2 (Mouse Delta / Right Stick)

    [Header("Position")]
    [SerializeField] private Vector3 rigLocalOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private bool rigIsChildOfPlayer = true;

    [Header("Mouse")]
    [SerializeField] private float sensitivityX = 180f;
    [SerializeField] private float sensitivityY = 180f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Optional")]
    [SerializeField] private bool lockCursor = true;

    private float _yaw;
    private float _pitch;

    void OnEnable()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (look != null) look.action.Enable();

        var yawEuler = yawPivot.rotation.eulerAngles;
        _yaw = yawEuler.y;

        var pitchEuler = pitchPivot.localRotation.eulerAngles;
        _pitch = NormalizeAngle(pitchEuler.x);
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    void OnDisable()
    {
        if (look != null) look.action.Disable();
    }

    void LateUpdate()
    {
        if (player == null || yawPivot == null || pitchPivot == null || look == null)
            return;

        if (!rigIsChildOfPlayer)
            yawPivot.position = player.position + rigLocalOffset;

        Vector2 delta = look.action.ReadValue<Vector2>();

        _yaw   += delta.x * sensitivityX * Time.deltaTime;
        _pitch -= delta.y * sensitivityY * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        yawPivot.rotation = Quaternion.Euler(0f, _yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }
}
