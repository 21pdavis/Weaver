using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerCameraManager : MonoBehaviour
{
    private enum CameraMode
    {
        FirstPerson,
        Isometric
    }

    [Header("References")]
    [SerializeField]
    private CinemachineVirtualCamera isometricCamera;

    [SerializeField]
    private CinemachineVirtualCamera firstPersonCamera;

    [SerializeField]
    private GameObject HUD;

    private CameraMode mode;

    private void Start()
    {
        mode = CameraMode.Isometric;
    }

    public void DebugCameraSwitch(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        Debug.Log("Switching Camera");

        switch (mode)
        {
            // changing to isometric
            case CameraMode.FirstPerson:
                mode = CameraMode.Isometric;

                // transition camera
                firstPersonCamera.gameObject.SetActive(false);
                isometricCamera.gameObject.SetActive(true);

                // change movement mode
                GetComponent<PlayerMovement>().Isometric = true;

                // unlock cursor
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;
                HUD.SetActive(false);

                Camera.main.orthographic = false;
                break;
            // changing to first person
            case CameraMode.Isometric:
                mode = CameraMode.FirstPerson;

                // transition camera
                firstPersonCamera.gameObject.SetActive(true);
                isometricCamera.gameObject.SetActive(false);

                // change movement mode
                GetComponent<PlayerMovement>().Isometric = false;

                // lock cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true;
                HUD.SetActive(true);

                Camera.main.orthographic = false;
                break;
            default:
                break;
        }
    }
}
