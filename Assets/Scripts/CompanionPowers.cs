using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CompanionPowers : MonoBehaviour
{
    // refactor to selecting a power?
    public void Suspend(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        // TODO: figure out gamepad controls for this, too (need to reconcile lock-on with mouse)
        // raycast from mouse to world, get closest enemy
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // suspend first enemy hit in a sphere around the hit point
            Collider[] colliders = Physics.OverlapSphere(hit.point, 5f, LayerMask.GetMask("Enemy"));
            if (colliders.Length > 0)
            {
                colliders[0].GetComponent<EnemyMovement>().navMeshAgent.enabled = false;
                colliders[0].transform.position += new Vector3(0, 5f, 0);
                colliders[0].transform.Rotate(45f, 0, 0f, Space.Self);
            }
        }
    }
}
