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

    // TODO: rename this to something more accurate ("TopDown"?)
    public bool Isometric;

    private void Start()
    {
        isometricCamera.gameObject.SetActive(Isometric);
        firstPersonCamera.gameObject.SetActive(!Isometric);

        // lock or unlock cursor
        Cursor.lockState = Isometric ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Isometric;
        HUD.SetActive(!Isometric);

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
        firstPersonCamera.gameObject.SetActive(!Isometric);
        isometricCamera.gameObject.SetActive(Isometric);

        // lock or unlock cursor
        Cursor.lockState = Isometric ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Isometric;
        HUD.SetActive(!Isometric);

        // enable/disable player mesh
        foreach (var meshRenderer in playerMeshes)
        {
            meshRenderer.enabled = Isometric;
        }
    }
}
