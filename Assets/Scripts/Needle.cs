using System.Collections;
using UnityEngine;

public class Needle : MonoBehaviour
{
    public Transform needleBack;

    [SerializeField]
    private float flightSpeed;

    [SerializeField]
    private float mountSpeed;

    [SerializeField]
    private float launchDelay;

    // TODO: animation curve for mounting

    private MeshRenderer meshRenderer;
    private Vector3 prevTip;
    private bool firing;

    /// <summary>
    /// Point to which the needle will travel before shooting forward
    /// </summary>
    private Vector3 launchPoint;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        prevTip = transform.position;
        firing = false;
    }

    void Update()
    {
        if (firing && !DetectContinuousCollision())
        {
            transform.position += flightSpeed * Time.deltaTime * transform.forward;
            prevTip = transform.position + meshRenderer.bounds.size.z / 4f * transform.forward;
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
        if (!firing)
            return false;

        Vector3 needleTip = transform.position + meshRenderer.bounds.size.z / 2f * transform.forward;
        Vector3 needleBase = transform.position - meshRenderer.bounds.size.z / 2f * transform.forward;
        Vector3 tipToBase = needleBase - needleTip;
        Vector3 baseToPrevTip = prevTip - needleBase;

        RaycastHit gapHit = new RaycastHit();
        if (Physics.Raycast(needleTip, tipToBase, out RaycastHit hit, tipToBase.magnitude) || Physics.Raycast(needleBase, baseToPrevTip, out gapHit, baseToPrevTip.magnitude))
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
