using System.Collections;
using UnityEngine;

public class NeedleTargeting : MonoBehaviour
{
    [SerializeField]
    private float flightSpeed;

    [SerializeField]
    private float mountSpeed;

    [SerializeField]
    private float launchDelay;

    // TODO: animation curve for mounting

    private Collider needleCollider;
    private bool firing;

    /// <summary>
    /// Point to which the needle will travel before shooting forward
    /// </summary>
    private Vector3 launchPoint;

    void Start()
    {
        needleCollider = GetComponent<Collider>();
        firing = false;
    }

    void Update()
    {
        if (firing)
        {
            transform.position += flightSpeed * Time.deltaTime * transform.forward;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // TODO: collisions on other types
        if (other.CompareTag("Enemy"))
        {
            // stick needle into enemy
            firing = false;
            needleCollider.enabled = false;

            transform.parent = other.transform;
        }
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

        needleCollider.enabled = true;
        firing = true;
    }

    internal void Fire(Vector3 launchPoint)
    {
        this.launchPoint = launchPoint;
        transform.rotation = Quaternion.LookRotation(launchPoint - Camera.main.transform.position);
        StartCoroutine(MountNeedleAtLaunchPoint());
    }
}
