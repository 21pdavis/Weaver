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

    private CompanionMovement companionMovement;

    private void Start()
    {
        companionMovement = GetComponent<CompanionMovement>();
    }

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

    /// <summary>
    /// Handle popping the enemy up into the air with the companion flying up with it in a circle
    /// </summary>
    /// <param name="suspendTarget">The enemy to suspend</param>
    /// <param name="startPosition">The position of the <see cref="suspendTarget"/> before the suspension</param>
    /// <param name="endPosition">The position of the <see cref="suspendTarget"/> after the suspension</param>
    /// <returns></returns>
    private IEnumerator SuspendObjectWithCompanionFlight(GameObject suspendTarget, Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 currPosition = startPosition;

        // TODO: smooth initial companion movement
        transform.position = suspendTarget.transform.position + (suspendTarget.GetComponent<MeshRenderer>().bounds.size.y / 2) * Vector3.down;

        while (endPosition.y - currPosition.y > 0.1f)
        {
            //currPosition = Vector3.MoveTowards(currPosition, endPosition, 5f * Time.deltaTime);
            currPosition = Vector3.Lerp(currPosition, endPosition, 5f * Time.deltaTime);
            suspendTarget.transform.position = currPosition;
            transform.position = currPosition - suspendTarget.GetComponent<MeshRenderer>().bounds.size.y / 1.5f * Vector3.up;
            yield return new WaitForEndOfFrame();
        }

        // TODO: need to re-attach to player after completing the animation (make this look nicer)
        companionMovement.enabled = true;

        StartCoroutine(CancelSuspendAfterDelay(suspendTarget));
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
                Rigidbody targetRb = suspendTarget.GetComponent<Rigidbody>();

                // place companion at base of target's mesh before suspending it
                companionMovement.enabled = false; // disable companion movement while suspended

                // disable navmesh to allow direct transform manipulation
                suspendTarget.GetComponent<EnemyMovement>().navMeshAgent.enabled = false;

                // need to zero velocities and disable gravity to prevent the enemy from having "left over" velocity from moving/previous suspend etc.
                targetRb.isKinematic = false;
                targetRb.velocity = Vector3.zero;
                targetRb.angularVelocity = -0.25f * suspendTarget.transform.right;
                targetRb.useGravity = false;

                StartCoroutine(SuspendObjectWithCompanionFlight(suspendTarget, suspendTarget.transform.position, suspendTarget.transform.position + new Vector3(0, suspendHeight, 0)));
            }
        }
    }
}
