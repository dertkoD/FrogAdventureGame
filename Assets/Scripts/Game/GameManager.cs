using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Input System (optional)")]
    [SerializeField] private InputActionReference restart; // Button action, bind to <Keyboard>/r

    [Header("Options")]
    [SerializeField] private bool lockCursorOnRestart = false;

    void OnEnable()
    {
        if (restart != null) restart.action.Enable();
    }

    void OnDisable()
    {
        if (restart != null) restart.action.Disable();
    }

    void Update()
    {
        if (restart != null)
        {
            if (restart.action.WasPressedThisFrame())
                ReloadCurrentScene();
            return;
        }
    }

    public void ReloadCurrentScene()
    {
        if (lockCursorOnRestart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
