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

    [SerializeField]
    private CinemachineBrain brain;

    // TODO: rename this to something more accurate ("TopDown"?)
    public bool Isometric;

    private PlayerMovement playerMovement;
    private PlayerNeedles playerNeedles;

    private void Awake()
    {
        // lock or unlock cursor
        Cursor.lockState = Isometric ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Isometric;
        HUD.SetActive(!Isometric);

        // transition camera (order actually matters here)
        isometricCamera.gameObject.SetActive(Isometric);
        firstPersonCamera.gameObject.SetActive(!Isometric);
        playerMovement = GetComponent<PlayerMovement>();
        playerNeedles = GetComponent<PlayerNeedles>();
    }

    private IEnumerator TemporarilyDisablePlayerMovement()
    {
        playerMovement.canMove = false;
        playerMovement.canLook = false;
        playerNeedles.canFire = false;
        playerMovement.moveDirection = Vector3.zero;
        yield return new WaitForEndOfFrame();

        while (brain.IsBlending)
        {
            yield return new WaitForEndOfFrame();
        }

        playerMovement.canMove = true;
        playerMovement.canLook = true;
        playerNeedles.canFire = true;
    }

    private void Update()
    {
        // TODO: better handling of Isometric --> First Person transition (disappears too early right now)
        if (playerMeshes.Count > 0)
        {
            if (
                Isometric && !playerMeshes[0].enabled
                ||
                !Isometric && playerMeshes[0].enabled
            )
            {
                // enable/disable player mesh
                foreach (var meshRenderer in playerMeshes)
                {
                    meshRenderer.enabled = Isometric;
                }

            }
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

        StartCoroutine(TemporarilyDisablePlayerMovement());

        // lock or unlock cursor
        Cursor.lockState = Isometric ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Isometric;
        HUD.SetActive(!Isometric);
    }
}
