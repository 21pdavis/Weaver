using System.Collections;
using UnityEngine;

public class Needle : MonoBehaviour
{
    internal Vector3 needleBack;
    internal Vector3 needleFront;
    internal bool firing;
    internal bool stuckIntoObject;
    internal bool grabbable;

    [SerializeField]
    private float flightSpeed;

    [SerializeField]
    private float mountSpeed;

    [SerializeField]
    private float launchDelay;

    private GameObject player;
    private PlayerNeedleController playerNeedleController;
    private MeshRenderer meshRenderer;
    private Vector3 prevFront;
    private float needleLength;
    private float initialFlightSpeed;

    /// <summary>
    /// Point to which the needle will travel before shooting forward
    /// </summary>
    //private Vector3 launchPoint;

    void Start()
    {
        player = GameObject.Find("Player");
        playerNeedleController = player.GetComponent<PlayerNeedleController>();
        needleLength = GameObject.Find("Reference Needle").GetComponent<MeshRenderer>().bounds.size.z;

        meshRenderer = GetComponent<MeshRenderer>();
        prevFront = transform.position;
        firing = false;
        initialFlightSpeed = flightSpeed;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.DrawWireCube(transform.position, meshRenderer.bounds.size);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(needleBack, 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(needleFront, 0.5f);

            Gizmos.color = Color.blue + Color.red;
            Gizmos.DrawWireSphere(prevFront, 0.5f);
        }
    }

    void Update()
    {
        needleFront = transform.position + (needleLength / 2f) * transform.forward;
        needleBack = transform.position - (needleLength / 2f) * transform.forward;

        if (firing && !DetectContinuousCollision())
        {
            // propel needle forward
            transform.position += flightSpeed * Time.deltaTime * transform.forward;
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
        if (!firing)
            return false;

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
        flightSpeed = initialFlightSpeed;

        transform.SetParent(other.localScale == Vector3.one ? other : other.parent, true);
    }

    private IEnumerator MountAndFire()
    {
        Vector3 launchPointAtTimeOfFire = playerNeedleController.launchPoint;

        while (Vector3.Distance(launchPointAtTimeOfFire, transform.position) > 0.1f)
        {
            // TODO: Slerp (idk that we do want a slerp here...)
            transform.position = Vector3.Lerp(transform.position, launchPointAtTimeOfFire, Time.deltaTime * flightSpeed);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(launchDelay);

        firing = true;
    }

    internal void Fire()
    {
        transform.rotation = Quaternion.LookRotation(playerNeedleController.launchPoint - Camera.main.transform.position);
        StartCoroutine(MountAndFire());
    }

    // TODO: put this in a static helpers class?
    private IEnumerator ShakeObject(float shakeDuration, float maxDistance, float frequency, float intensity = 0.1f)
    {
        Vector3 initialPosition = transform.position;
        float startTime = Time.time;

        while (Time.time < startTime + shakeDuration)
        {
            float elapsedTime = Time.time - startTime;

            float displacement = Mathf.Sin(elapsedTime * frequency * Mathf.PI * 2f) * maxDistance * intensity;
            float randomDirectionAngle = Random.Range(0f, 360f);
            //Vector3 newPosition = initialPosition + (Quaternion.AngleAxis(randomDirectionAngle, transform.forward) * new Vector3(displacement, displacement, 0f));
            Vector3 newPosition = playerNeedleController.launchPoint + (Quaternion.AngleAxis(randomDirectionAngle, transform.forward) * new Vector3(displacement, displacement, 0f));
            transform.rotation = Quaternion.LookRotation(playerNeedleController.launchPoint - Camera.main.transform.position);
            transform.position = newPosition;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator MountAndPowerFire()
    {
        Vector3 launchPointAtTimeOfFire = playerNeedleController.launchPoint;
        playerNeedleController.canFire = false;

        while (Vector3.Distance(launchPointAtTimeOfFire, transform.position) > 0.1f)
        {
            // TODO: Slerp (idk that we do want a slerp here...)
            transform.position = Vector3.Lerp(transform.position, launchPointAtTimeOfFire, Time.deltaTime * flightSpeed);
            yield return new WaitForEndOfFrame();
        }

        StartCoroutine(ShakeObject(playerNeedleController.powerFireChargeTime, 0.5f, 10f));
        yield return new WaitForSeconds(playerNeedleController.powerFireChargeTime);

        playerNeedleController.canFire = true;
        firing = true;
    }

    internal void PowerFire()
    {
        transform.rotation = Quaternion.LookRotation(playerNeedleController.launchPoint - Camera.main.transform.position);
        StartCoroutine(MountAndPowerFire());
    }
}
