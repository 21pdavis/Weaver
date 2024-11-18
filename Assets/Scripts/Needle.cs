using System.Collections;
using UnityEngine;

public class Needle : MonoBehaviour
{
    internal Vector3 needleBack;
    internal Vector3 needleFront;
    internal bool firing;
    internal bool stuckIntoObject;

    [SerializeField]
    private float flightSpeed;

    [SerializeField]
    private float mountSpeed;

    [SerializeField]
    private float launchDelay;

    private MeshRenderer meshRenderer;
    private Vector3 prevFront;

    /// <summary>
    /// Point to which the needle will travel before shooting forward
    /// </summary>
    private Vector3 launchPoint;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        prevFront = transform.position;
        firing = false;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.DrawWireCube(transform.position, meshRenderer.bounds.size);

            Gizmos.DrawWireSphere(needleBack, 0.5f);
            Gizmos.DrawWireSphere(needleFront, 0.5f);
            Gizmos.DrawWireSphere(prevFront, 0.5f);
        }
    }

    void Update()
    {
        needleFront = transform.position + (meshRenderer.bounds.size.z / 2f) * transform.forward;
        needleBack = transform.position - (meshRenderer.bounds.size.z / 2f) * transform.forward;

        // ! DEBUG
        //DetectContinuousCollision();
        //firing = true;
        // ! END DEBUG

        if (firing && !DetectContinuousCollision())
        {
            // propel needle forward
            transform.position += flightSpeed * Time.deltaTime * transform.forward;
            //transform.position += flightSpeed / 20 * Time.deltaTime * transform.forward; // ! DELETE
            //transform.Rotate(Vector3.up, -10 * Time.deltaTime);
            prevFront = transform.position + meshRenderer.bounds.size.z / 2f * transform.forward;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!firing || other.CompareTag("Player") || other.CompareTag("Needle"))
            return;

        StickInto(transform.position, other.transform);
    }

    private bool DetectContinuousCollision()
    {
        //if (!firing)
        //    return false;

        Vector3 frontToBack = (needleBack - needleFront).normalized;
        Vector3 backToPrevFront = (prevFront - needleBack).normalized;

        RaycastHit gapHit = new();
        if (
            Physics.Raycast(needleFront, frontToBack, out RaycastHit hit, frontToBack.magnitude)
            || Physics.Raycast(needleBack, backToPrevFront, out gapHit, backToPrevFront.magnitude))
        {
            RaycastHit realHit = gapHit.collider ? gapHit : hit;

            if (realHit.collider.CompareTag("Player") || realHit.collider.CompareTag("Needle"))
                return false;

            StickInto(realHit.point, realHit.transform);
            return true;
        }

        return false;
    }

    private void StickInto(Vector3 point, Transform other)
    {
        firing = false;
        stuckIntoObject = true;
        transform.position = point;

        transform.SetParent(other.localScale == Vector3.one ? other : other.parent, true);
    }

    private IEnumerator MountNeedleAtLaunchPoint()
    {
        while (Vector3.Distance(launchPoint, transform.position) > 0.1f)
        {
            // TODO: Slerp
            transform.position = Vector3.Lerp(transform.position, launchPoint, Time.deltaTime * flightSpeed);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(launchDelay);

        firing = true;
    }

    internal void Fire(Vector3 launchPoint)
    {
        this.launchPoint = launchPoint;
        transform.rotation = Quaternion.LookRotation(launchPoint - Camera.main.transform.position);
        StartCoroutine(MountNeedleAtLaunchPoint());
    }
}
