using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private float rotationSpeed;

    private Vector3 moveDirection;
    private CharacterController controller;

    // Start is called before the first frame update
    void Start()
    {
        moveDirection = Vector3.zero;
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        controller.Move(moveSpeed * Time.deltaTime * moveDirection);

        // rotate character in direction of movement
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            // TODO: adjust for framerate
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 inputDirection = context.ReadValue<Vector2>();
            Vector3 inCameraDirection = Camera.main.transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y));
            //Vector3 inCameraDirection = Camera.main.transform.TransformDirection(new Vector3(inputDirection.x, 0, inputDirection.y));
            moveDirection = new Vector3(inCameraDirection.x, 0, inCameraDirection.z);
        }
        else if (context.canceled)
        {
            moveDirection = Vector3.zero;
        }
    }
}
