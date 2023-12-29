using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CompanionPowers : MonoBehaviour
{
    [Header("Suspend Options")]
    [SerializeField]
    private float suspendDuration;

    [SerializeField]
    private float suspendHeight;

    private IEnumerator ReEnableNavMeshOnGrounded(GameObject suspendTarget)
    {
        bool grounded = false;
        MeshRenderer targetMeshRenderer = suspendTarget.GetComponent<MeshRenderer>();

        while (!grounded)
        {
            grounded = Physics.Raycast(targetMeshRenderer.bounds.center, targetMeshRenderer.bounds.center + (targetMeshRenderer.bounds.size.y / 2 + 0.1f) * Vector3.down);
            Debug.DrawRay(targetMeshRenderer.bounds.center, (targetMeshRenderer.bounds.size.y / 2 + 0.1f) * Vector3.down, Color.red, .1f);
            yield return null;
        }

        suspendTarget.GetComponent<EnemyMovement>().navMeshAgent.enabled = true;
        // TODO: should point at player, not companion ideally
        suspendTarget.transform.rotation = Quaternion.LookRotation(transform.position - suspendTarget.transform.position);
        suspendTarget.GetComponent<Rigidbody>().isKinematic = true;
    }

    private IEnumerator CancelSuspendAfterDelay(GameObject suspendTarget)
    {
        yield return new WaitForSeconds(suspendDuration);

        Rigidbody targetRb = suspendTarget.GetComponent<Rigidbody>();
        targetRb.useGravity = true;
        targetRb.angularVelocity = -5f * suspendTarget.transform.right;
        StartCoroutine(ReEnableNavMeshOnGrounded(suspendTarget));
    }

    // TODO: refactor to activate selected power?
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
                GameObject suspendTarget = colliders[0].gameObject;
                Transform targetTransform = suspendTarget.transform;
                Rigidbody targetRb = suspendTarget.GetComponent<Rigidbody>();

                suspendTarget.GetComponent<EnemyMovement>().navMeshAgent.enabled = false;
                targetTransform.position += new Vector3(0, suspendHeight, 0);
                targetTransform.Rotate(-45f, 0, 0f, Space.Self);

                // need to zero velocities and disable gravity to prevent the enemy from having "left over" velocity from moving/previous suspend etc.
                targetRb.isKinematic = false;
                targetRb.velocity = Vector3.zero;
                targetRb.angularVelocity = -0.25f * targetTransform.right;
                targetRb.useGravity = false;

                StartCoroutine(CancelSuspendAfterDelay(suspendTarget));
            }
        }
    }
}
