using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraManager : MonoBehaviour
{
    private enum CameraMode
    {
        FirstPerson,
        Isometric
    }

    private CameraMode mode;

    private void Start()
    {
        mode = CameraMode.Isometric;
    }

    private void ToFirstPerson()
    {

    }

    private void FromFirstPerson()
    {

    }

    private void ToIsometric()
    {

    }

    private void FromIsometric()
    {

    }

    public void DebugCameraSwitch(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        Debug.Log("Switching Camera");

        switch (mode)
        {
            case CameraMode.FirstPerson:
                mode = CameraMode.Isometric;
                FromFirstPerson();
                ToIsometric();
                break;
            case CameraMode.Isometric:
                mode = CameraMode.FirstPerson;
                FromIsometric();
                ToFirstPerson();
                break;
            default:
                break;
        }
    }
}
