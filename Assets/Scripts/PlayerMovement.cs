using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [SerializeField]
    private float slideCameraOffset;

    [SerializeField]
    private float slideCameraDropSpeed;

    [SerializeField]
    private float slideDuration;

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

    /// <summary>
    /// A pivot point with only the y-rotation of the isometric camera applied, helps simplify movement from cam's perspective
    /// </summary>
    [SerializeField]
    private Transform isometricCameraPivot;

    private CharacterController controller;
    private MeshRenderer meshRenderer;
    private PlayerCameraManager cameraManager;

    internal Vector3 moveDirection;
    internal bool canMove;
    internal bool canLook;
    internal bool grounded;
    internal float horizontalMoveSpeedMultiplier;
    internal float verticalMoveSpeedMultiplier;

    private Vector3 firstPersonLookDirection;
    private float verticalVelocity;
    private bool waitingForJump;
    private bool sprinting;
    private bool sliding;
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
        canMove = true;
        canLook = true;
        grounded = true;
        waitingForJump = false;
        horizontalMoveSpeedMultiplier = 1.0f;
        verticalMoveSpeedMultiplier = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGrounded();

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

        // canLook refers to either: Player rotation when in isometric mode, OR Camera rotation when in first person mode
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
                transform.Rotate(cameraManager.firstPersonSensitivity * new Vector3(0f, firstPersonLookDirection.x, 0f));

                // up-and-down rotation
                Vector3 xRotationDelta = cameraManager.firstPersonSensitivity * new Vector3(-firstPersonLookDirection.y, 0f, 0f);
                if (
                    (cameraManager.GetActiveCamera().transform.rotation.eulerAngles + xRotationDelta).x < 90f
                    || (cameraManager.GetActiveCamera().transform.rotation.eulerAngles + xRotationDelta).x > 270f
                )
                {
                    cameraManager.GetActiveCamera().transform.Rotate(xRotationDelta);
                }
            }
        }

        // move character
        if (canMove)
        {
            controller.Move(
                moveSpeed * Time.deltaTime * horizontalMoveSpeedMultiplier * moveDirection // horizontal
                + verticalVelocity * verticalMoveSpeedMultiplier * Time.deltaTime * Vector3.up // vertical
            );
        }
    }
    
    private void FixedUpdate()
    {
        UpdateGravity();
    }

    private void UpdateGrounded()
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
            Vector3 inCameraDirection = isometricCameraPivot.transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y)).normalized;
            moveDirection = new Vector3(inCameraDirection.x, 0, inCameraDirection.z).normalized;
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
            moveDirection = Quaternion.AngleAxis(cameraManager.firstPersonSensitivity * firstPersonLookDirection.x, Vector3.up) * moveDirection;
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

    // TODO: make boost more expressive + directional
    public void Boost(Vector3 boostVec)
    {
        //! note to self: maybe do some form of += here, could be cool to have a multiplier, but straight += seems like it could be too much
        verticalVelocity = jumpStrength * 2 * boostVec.y;
    }

    private IEnumerator SlideRoutine()
    {
        CinemachineVirtualCamera cam = cameraManager.GetActiveCamera();
        Vector3 camStartPos = cam.transform.position;
        Vector3 camEndPos = cam.transform.position + slideCameraOffset * Vector3.down;

        if (!cameraManager.Isometric)
        {
            while (Mathf.Abs(cam.transform.position.y - camEndPos.y) > 0.01f)
            {
                float currY = Mathf.Lerp(cam.transform.position.y, camEndPos.y, slideCameraDropSpeed * Time.deltaTime);
                cam.transform.position = new Vector3(cam.transform.position.x, currY, cam.transform.position.z);

                yield return new WaitForEndOfFrame();
            }
        }

        yield return new WaitForSeconds(slideDuration);

        if (!cameraManager.Isometric)
        {
            while (Mathf.Abs(cam.transform.position.y - camStartPos.y) > 0.01f)
            {
                float currY = Mathf.Lerp(cam.transform.position.y, camStartPos.y, 4 * slideCameraDropSpeed * Time.deltaTime);
                cam.transform.position = new Vector3(cam.transform.position.x, currY, cam.transform.position.z);
                yield return new WaitForEndOfFrame();
            }
        }

        sliding = false;
    }

    public void Slide(InputAction.CallbackContext context)
    {
        if (!context.started || !grounded || sliding || moveDirection == Vector3.zero)
            return;

        sliding = true;
        StartCoroutine(SlideRoutine());
    }

    private IEnumerator ZoomOutCamera()
    {
        float zoomedOutFOV = cameraManager.GetNormalLensSize() * sprintZoomMultiplier;
        while (zoomedOutFOV - cameraManager.GetCurrentLensSize() > 0.1f)
        {
            cameraManager.SetLensSize(Mathf.Lerp(cameraManager.GetCurrentLensSize(), zoomedOutFOV, sprintZoomSpeed * Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        cameraManager.SetLensSize(cameraManager.GetNormalLensSize() * sprintZoomMultiplier);
    }

    private IEnumerator ZoomInCamera()
    {
        while (cameraManager.GetCurrentLensSize() - cameraManager.GetNormalLensSize() > 0.1f)
        {
            cameraManager.SetLensSize(Mathf.Lerp(cameraManager.GetCurrentLensSize(), cameraManager.GetNormalLensSize(), sprintZoomSpeed * 2 * Time.deltaTime));
            yield return new WaitForEndOfFrame();
        }

        cameraManager.SetLensSize(cameraManager.GetNormalLensSize());
    }

    public void Sprint(InputAction.CallbackContext context)
    {
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
    }
}
