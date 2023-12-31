using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Options")]
    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private float jumpStrength;

    [SerializeField]
    private float gravityMagnitude;

    [Header("Camera Options")]
    [SerializeField]
    private CinemachineVirtualCamera isometricCamera;

    [SerializeField]
    private CinemachineVirtualCamera firstPersonCamera;

    [SerializeField]
    private float firstPersonSensitivity;

    [SerializeField]
    [Tooltip("How fast the camera zooms out when sprinting.")]
    private float sprintZoomSpeed;

    [SerializeField]
    [Tooltip("How much the camera zooms out when sprinting.")]
    private float sprintZoomMultiplier;

    [Header("References")]
    [SerializeField]
    [Tooltip("The physical object that represents the player")]
    private GameObject playerObject;

    [SerializeField]
    private ParticleSystem sprintParticles;

    private CharacterController controller;
    private MeshRenderer meshRenderer;
    private PlayerCameraManager cameraManager;

    internal bool canMove;
    internal bool canLook;
    internal Vector3 moveDirection;

    private Vector3 firstPersonLookDirection;
    private float verticalVelocity;
    private float normalCameraLensSize;
    private bool grounded;
    private bool waitingForJump;
    private bool sprinting;
    private IEnumerator cameraZoomOutHandle;
    private IEnumerator cameraZoomInHandle;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        meshRenderer = playerObject.GetComponent<MeshRenderer>();
        cameraManager = GetComponent<PlayerCameraManager>();

        moveDirection = Vector3.zero;
        verticalVelocity = -0.5f;
        normalCameraLensSize = isometricCamera.m_Lens.OrthographicSize;
        canMove = true;
        canLook = true;
        grounded = true;
        waitingForJump = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateIsGrounded();

        // check if sprinting and grounded, play or stop particles accordingly
        if (sprinting)
        {
            if (!grounded && sprintParticles.isPlaying)
            {
                sprintParticles.Stop();
            }
            else if (grounded && !sprintParticles.isPlaying)
            {
                sprintParticles.Play();
            }
        }

        if (canLook)
        {
            // rotate character in direction of movement
            if (cameraManager.Isometric && moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                // TODO: Determine if I want to lerp or not here (probably not for more responsive movement)
                transform.rotation = targetRotation;
            }
            // rotate character in direction of camera
            else if (!cameraManager.Isometric)
            {
                // side-to-side rotation
                transform.Rotate(firstPersonSensitivity * new Vector3(0f, firstPersonLookDirection.x, 0f));

                // up-and-down rotation
                Vector3 xRotationDelta = firstPersonSensitivity * new Vector3(-firstPersonLookDirection.y, 0f, 0f);
                if (
                    (firstPersonCamera.transform.rotation.eulerAngles + xRotationDelta).x < 90f
                    || (firstPersonCamera.transform.rotation.eulerAngles + xRotationDelta).x > 270f
                )
                {
                    firstPersonCamera.transform.Rotate(xRotationDelta);
                }
            }
        }

        // move character
        if (canMove)
        {
           controller.Move(moveSpeed * Time.deltaTime * moveDirection + verticalVelocity * Time.deltaTime * Vector3.up);
        }
    }
    
    private void FixedUpdate()
    {
        UpdateGravity();
    }

    private void UpdateIsGrounded()
    {
        // raycast down to check if grounded
        Bounds meshBounds = meshRenderer.bounds;
        Ray ray = new Ray(meshBounds.center, Vector3.down);
        grounded = Physics.Raycast(ray, meshBounds.size.y / 2 + 0.1f);
    }

    private void UpdateGravity()
    {
        if (grounded && !waitingForJump)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            // apply gravity with terminal velocity
            verticalVelocity = Mathf.Clamp(verticalVelocity - gravityMagnitude * Time.deltaTime, -120f, 120f);
        }
    }

    private void MoveIsometric(InputAction.CallbackContext context)
    {
        // using performed instead of started here helps to detect for changes in how far the user is pushing the stick
        if (context.performed)
        {
            Vector2 inputDirection = context.ReadValue<Vector2>();
            Vector3 inCameraDirection = Camera.main.transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y));
            moveDirection = new Vector3(inCameraDirection.x, 0, inCameraDirection.z);
        }
        else if (context.canceled)
        {
            moveDirection = Vector3.zero;
        }
    }

    private void MoveFirstPerson(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 inputDirection = context.ReadValue<Vector2>();
            moveDirection = Camera.main.transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y));

            // flatten out moveDirection
            moveDirection.y = 0f;
            moveDirection.Normalize();
        }
        else if (context.canceled)
        {
            moveDirection = Vector3.zero;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (cameraManager.Isometric)
        {
            MoveIsometric(context);
        }
        else
        {
            MoveFirstPerson(context);
        }
    }

    public void Look(InputAction.CallbackContext context)
    {
        if (cameraManager.Isometric)
            return;

        if (context.performed)
        {
            firstPersonLookDirection = context.ReadValue<Vector2>();
            moveDirection = Quaternion.AngleAxis(firstPersonSensitivity * firstPersonLookDirection.x, Vector3.up) * moveDirection;
        }
        else if (context.canceled)
        {
            firstPersonLookDirection = Vector3.zero;
        }
    }

    private IEnumerator DisableJumpingWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        waitingForJump = false;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && grounded)
        {
            waitingForJump = true;
            verticalVelocity = jumpStrength;
            StartCoroutine(DisableJumpingWithDelay());
        }
    }

    private IEnumerator ZoomOutCamera()
    {
        while (isometricCamera.m_Lens.OrthographicSize < normalCameraLensSize * sprintZoomMultiplier)
        {
            isometricCamera.m_Lens.OrthographicSize += sprintZoomSpeed * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        isometricCamera.m_Lens.OrthographicSize = normalCameraLensSize * sprintZoomMultiplier;
    }

    private IEnumerator ZoomInCamera()
    {
        while (isometricCamera.m_Lens.OrthographicSize > normalCameraLensSize)
        {
            isometricCamera.m_Lens.OrthographicSize -= sprintZoomSpeed / 2 * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        isometricCamera.m_Lens.OrthographicSize = normalCameraLensSize;
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        // TODO: toggle vs hold to sprint (see code below, can just add a toggle vs hold flag)
        if (context.started)
        {
            sprinting = true;

            if (cameraZoomInHandle != null)
            {
                StopCoroutine(cameraZoomInHandle);
            }

            cameraZoomOutHandle = ZoomOutCamera();
            StartCoroutine(cameraZoomOutHandle);

            sprintParticles.Play();
            moveSpeed *= 2;
        }
        else if (context.canceled)
        {
            sprinting = false;

            if (cameraZoomOutHandle != null)
            {
                StopCoroutine(cameraZoomOutHandle);
            }

            cameraZoomInHandle = ZoomInCamera();
            StartCoroutine(cameraZoomInHandle);

            sprintParticles.Stop();
            moveSpeed /= 2;
        }


        //if (context.started)
        //{
        //    if (!sprinting)
        //    {
        //        sprinting = true;
        //        if (cameraZoomInHandle != null)
        //        {
        //            StopCoroutine(cameraZoomInHandle);
        //        }

        //        cameraZoomOutHandle = ZoomOutCamera();
        //        StartCoroutine(cameraZoomOutHandle);

        //        sprintParticles.Play();
        //        moveSpeed *= 2;
        //    }
        //    else
        //    {
        //        sprinting = false;
        //        if (cameraZoomOutHandle != null)
        //        {
        //            StopCoroutine(cameraZoomOutHandle);
        //        }

        //        cameraZoomInHandle = ZoomInCamera();
        //        StartCoroutine(cameraZoomInHandle);

        //        sprintParticles.Stop();
        //        moveSpeed /= 2;
        //    }
        //}
    }
}
