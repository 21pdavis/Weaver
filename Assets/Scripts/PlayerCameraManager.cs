using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerCameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private List<MeshRenderer> playerMeshes;

    [SerializeField]
    private CinemachineVirtualCamera isometricCamera;

    [SerializeField]
    private CinemachineVirtualCamera firstPersonCamera;

    [SerializeField]
    private GameObject HUD;

    [SerializeField]
    private Transform firstPersonCameraMountPoint;

    // TODO: rename this to something more accurate ("TopDown"?)
    public bool Isometric;

    private void Awake()
    {
        // lock or unlock cursor
        Cursor.lockState = Isometric ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Isometric;
        HUD.SetActive(!Isometric);

        // transition camera (order actually matters here)
        isometricCamera.gameObject.SetActive(Isometric);
        firstPersonCamera.gameObject.SetActive(!Isometric);

        // enable/disable player mesh
        foreach (var meshRenderer in playerMeshes)
        {
            meshRenderer.enabled = Isometric;
        }
    }

    public void DebugCameraSwitch(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        Debug.Log("Switching Camera");

        Isometric = !Isometric;

        // transition camera
        isometricCamera.gameObject.SetActive(Isometric);
        firstPersonCamera.gameObject.SetActive(!Isometric);

        // lock or unlock cursor
        Cursor.lockState = Isometric ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Isometric;
        HUD.SetActive(!Isometric);

        // enable/disable player mesh (TODO: need to delay this)
        foreach (var meshRenderer in playerMeshes)
        {
            meshRenderer.enabled = Isometric;
        }
    }
}
