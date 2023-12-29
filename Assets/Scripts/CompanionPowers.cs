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

    [SerializeField]
    private float suspendRaiseSpeed;

    [Header("References")]
    [SerializeField]
    private ParticleSystem suspendParticles;

    private CompanionMovement companionMovement;
    private GameObject activeSuspendParticles;
    bool currentlySuspending;

    private void Start()
    {
        companionMovement = GetComponent<CompanionMovement>();
        currentlySuspending = false;
    }

    private IEnumerator ReEnableNavMeshOnGrounded(GameObject suspendTarget)
    {
        bool grounded = false;
        MeshRenderer targetMeshRenderer = suspendTarget.GetComponent<MeshRenderer>();

        while (!grounded)
        {
            grounded = Physics.Raycast(targetMeshRenderer.bounds.center, targetMeshRenderer.bounds.center + (targetMeshRenderer.bounds.size.y / 2 + 0.1f) * Vector3.down);
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

        currentlySuspending = false;

        Rigidbody targetRb = suspendTarget.GetComponent<Rigidbody>();
        targetRb.useGravity = true;
        targetRb.angularVelocity = -5f * suspendTarget.transform.right;

        IEnumerator DestroyParticlesAfterDelay(GameObject particles)
        {
            yield return new WaitForSeconds(5f);
            Destroy(particles);
        }

        ParticleSystem.EmissionModule emission = activeSuspendParticles.GetComponent<ParticleSystem>().emission;
        emission.enabled = false;

        StartCoroutine(DestroyParticlesAfterDelay(activeSuspendParticles));
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
        // determine where companion should start for spiral around target
        MeshRenderer targetMeshRenderer = suspendTarget.GetComponent<MeshRenderer>();
        Vector3 flattenedTargetForward = new Vector3(suspendTarget.transform.forward.x, 0f, suspendTarget.transform.forward.z).normalized;
        Vector3 companionStartPosition = suspendTarget.transform.position
            + (targetMeshRenderer.bounds.size.y / 2f) * Vector3.down
            + (targetMeshRenderer.bounds.size.z / 1.5f) * flattenedTargetForward;

        // smoothly move companion to starting position
        while (Vector3.Distance(companionStartPosition, transform.position) > 0.1f)
        {
            // currently using a magic number for speed, is okay for now
            transform.position = Vector3.MoveTowards(transform.position, companionStartPosition, 7.5f * suspendRaiseSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        // for some reason, have to use "this" here, I think it's a compiler bug...? Probably not a bug, but I don't understand it
        Vector3 particleStartPosition = suspendTarget.transform.position + (targetMeshRenderer.bounds.size.y / 2f) * Vector3.down;
        activeSuspendParticles = Instantiate(this.suspendParticles.gameObject, particleStartPosition, Quaternion.Euler(-90f, 0f, 0f));

        Vector3 currTargetPosition = startPosition;
        //Vector3 currCompanionPosition = transform.position;
        // gradual raising of target happens here
        while (endPosition.y - currTargetPosition.y > 0.1f)
        {
            //currPosition = Vector3.MoveTowards(currPosition, endPosition, 15f * Time.deltaTime);
            currTargetPosition = Vector3.Lerp(currTargetPosition, endPosition, suspendRaiseSpeed * Time.deltaTime);

            // raise target 
            suspendTarget.transform.position = currTargetPosition;

            // advance companion on upward spiral based on how much we've raised the target
            float radians = Mathf.Deg2Rad * ((currTargetPosition.y - startPosition.y) / suspendHeight) * 360f;
            Vector3 unitCircleVector = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            Vector3 currCompanionPosition = startPosition + unitCircleVector;

            // now lift companion up to target's height
            currCompanionPosition.y += (currTargetPosition - startPosition).y;

            // offset down by 1/2 height of target
            currCompanionPosition.y -= targetMeshRenderer.bounds.size.y / 2f;

            transform.position = currCompanionPosition;

            yield return new WaitForEndOfFrame();
        }

        companionMovement.enabled = true;
        StartCoroutine(CancelSuspendAfterDelay(suspendTarget));
    }

    // TODO: refactor to activate selected power?
    public void Suspend(InputAction.CallbackContext context)
    {
        if (!context.started || currentlySuspending)
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
                currentlySuspending = true;

                GameObject suspendTarget = colliders
                    .OrderBy(collider => Vector3.Distance(hit.point, collider.transform.position))
                    .First()
                    .gameObject;

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
