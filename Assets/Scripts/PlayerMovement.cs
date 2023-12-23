using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private float rotationSpeed;

    [SerializeField]
    private float jumpStrength;

    [SerializeField]
    private float gravityMagnitude;

    private CharacterController controller;
    private MeshRenderer meshRenderer;

    private Vector3 moveDirection;
    private float verticalVelocity;
    private bool isGrounded;
    private bool waitingForJump;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        meshRenderer = GetComponent<MeshRenderer>();

        moveDirection = Vector3.zero;
        verticalVelocity = -0.5f;
        isGrounded = true;
        waitingForJump = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateIsGrounded();

        controller.Move(moveSpeed * Time.deltaTime * moveDirection + verticalVelocity * Time.deltaTime * Vector3.up);

        // rotate character in direction of movement
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
        isGrounded = Physics.Raycast(ray, meshBounds.size.y / 2 + 0.1f);
    }

    private void UpdateGravity()
    {
        if (isGrounded && !waitingForJump)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            // apply gravity with terminal velocity
            verticalVelocity = Mathf.Clamp(verticalVelocity - gravityMagnitude * Time.deltaTime, -120f, 120f);
        }
    }

    public void Move(InputAction.CallbackContext context)
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

    private IEnumerator DisableJumpingWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        waitingForJump = false;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded)
        {
            waitingForJump = true;
            verticalVelocity = jumpStrength;
            StartCoroutine(DisableJumpingWithDelay());
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        // TODO: toggle vs hold to sprint
        if (context.started)
        {
            moveSpeed *= 2;
        }
        else if (context.canceled)
        {
            moveSpeed /= 2;
        }
    }
}
