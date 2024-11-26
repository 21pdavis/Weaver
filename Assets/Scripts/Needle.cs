using System.Collections;
using UnityEngine;

public class Needle : MonoBehaviour
{
    internal enum NeedleState
    {
        Loaded,
        Firing,
        PowerFiring,
        Stuck
    }

    internal NeedleState state;

    internal Vector3 needleBack;
    internal Vector3 needleFront;
    internal bool grabbable;

    [SerializeField]
    private float flightSpeed;

    [SerializeField]
    private float mountSpeed;

    public MeshRenderer meshRenderer;

    private GameObject player;
    private PlayerNeedleController playerNeedleController;
    private Vector3 prevFront;
    private float needleLength;
    private float initialFlightSpeed;

    void Start()
    {
        state = NeedleState.Loaded;
        player = GameObject.Find("Player");
        playerNeedleController = player.GetComponent<PlayerNeedleController>();
        needleLength = GameObject.Find("Reference Needle").GetComponent<Needle>().meshRenderer.bounds.size.z;
        grabbable = false;

        prevFront = transform.position;
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

        if ((state == NeedleState.Firing || state == NeedleState.PowerFiring) && !DetectContinuousCollision())
        {
                // propel needle forward
                transform.position += flightSpeed * Time.deltaTime * transform.forward;
            prevFront = transform.position + meshRenderer.bounds.size.z / 2f * transform.forward;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!(state == NeedleState.Firing || state == NeedleState.PowerFiring) || other.CompareTag("Player") || other.CompareTag("Needle"))
            return;

        StickInto(transform.position, other.transform.parent);
    }

    private bool DetectContinuousCollision()
    {
        if (!(state == NeedleState.Firing || state == NeedleState.PowerFiring))
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
        transform.position = point;
        flightSpeed = initialFlightSpeed;

        if (state == NeedleState.PowerFiring && other.CompareTag("Enemy"))
        {
            EnemyStats enemyStats = other.GetComponent<EnemyStats>(); // assumption: all enemies have EnemyStats
            if (enemyStats && enemyStats.pinnable)
            {
                other.transform.rotation = Quaternion.LookRotation(-transform.forward, transform.up);
                other.transform.SetParent(transform, true);
                other.transform.position = Vector3.zero;
            }
            else
            {
                Debug.Log($"enemyStats is {enemyStats}");
            }
        }
        else
        {
            transform.SetParent(other.localScale == Vector3.one ? other : other.parent, true);
            state = NeedleState.Stuck;
        }
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

        state = NeedleState.Firing;
    }

    internal void Fire()
    {
        transform.rotation = Quaternion.LookRotation(playerNeedleController.launchPoint - Camera.main.transform.position);
        StartCoroutine(MountAndFire());
    }

    // TODO: put this in a static helpers class?
    private IEnumerator ShakeObject(float shakeDuration, float maxDistance, float frequency, float intensity = 0.1f)
    {
        float startTime = Time.time;

        while (Time.time < startTime + shakeDuration)
        {
            float elapsedTime = Time.time - startTime;

            float displacement = Mathf.Sin(elapsedTime * frequency * Mathf.PI * 2f) * maxDistance * intensity;
            float randomDirectionAngle = Random.Range(0f, 360f);
            //Vector3 newPosition = initialPosition + (Quaternion.AngleAxis(randomDirectionAngle, transform.forward) * new Vector3(displacement, displacement, 0f));
            Vector3 newPosition = playerNeedleController.launchPoint + (Quaternion.AngleAxis(randomDirectionAngle, transform.forward) * new Vector3(displacement, displacement, 0f));
            transform.SetPositionAndRotation(newPosition, Quaternion.LookRotation(playerNeedleController.launchPoint - Camera.main.transform.position));
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
        state = NeedleState.PowerFiring;
    }

    internal void PowerFire()
    {
        transform.rotation = Quaternion.LookRotation(playerNeedleController.launchPoint - Camera.main.transform.position);
        StartCoroutine(MountAndPowerFire());
    }
}
