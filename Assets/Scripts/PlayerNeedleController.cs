using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using static Helpers;

public class PlayerNeedles : MonoBehaviour
{
    [Header("Needle Options")]
    [SerializeField]
    private int maxNeedles;

    [SerializeField]
    private float spreadHorizontal;

    [SerializeField]
    private float spreadVertical;

    [SerializeField]
    private float distanceFromPlayer;

    [SerializeField]
    private float followSpeed;

    [SerializeField]
    private float fireSpeed;

    [SerializeField]
    private float retrievalSpeed;

    [SerializeField]
    [Tooltip("Set to 0 to disable needle regen")]
    private float needleRegenTime;

    [SerializeField]
    private float grabDistance;

    [SerializeField]
    private float grabRadius;

    [Header("References")]
    [SerializeField]
    private Transform threadStartPoint;

    [SerializeField]
    private GameObject playerMesh;

    [SerializeField]
    private GameObject needlePrefab;

    internal List<GameObject> Needles { get; set; }

    private MeshRenderer meshRenderer;
    private PlayerCameraManager cameraManager;

    internal bool canFire;

    private GameObject grabbedNeedle;
    private Vector3 needleAnchorCenter;
    private List<Vector3> needlePositions;
    private float radianIncrement;
    private float regenStartTime;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = playerMesh.GetComponent<MeshRenderer>();
        cameraManager = GetComponent<PlayerCameraManager>();
        UpdateAnchors();

        // initial needle positions
        needlePositions = new List<Vector3>();
        radianIncrement = Mathf.Deg2Rad * (180f / (maxNeedles - 1));
        for (int i = 0; i < maxNeedles; ++i)
        {
            Vector3 needlePosition = needleAnchorCenter + transform.TransformVector(new Vector3(
                Mathf.Cos(radianIncrement * i) * spreadHorizontal / 2,
                Mathf.Sin(radianIncrement * i) * spreadVertical / 2, // unsure of why I have to divide by 2 here
                0
            ));
            needlePositions.Add(needlePosition);
        }

        // instantiate needles
        Needles = new List<GameObject>();
        for (int i = 0; i < maxNeedles; ++i)
        {
            GameObject needle = Instantiate(needlePrefab, needlePositions[i], transform.rotation);
            Needles.Add(needle);
        }

        canFire = true;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnchors();

        if (needleRegenTime > 0 && Needles.Count < maxNeedles && Time.time > regenStartTime + needleRegenTime)
        {
            regenStartTime = Time.time;
            Needles.Add(Instantiate(needlePrefab, needlePositions[Needles.Count], transform.rotation));
        }

        // determine next needle positions
        List<Vector3> prevPositions = new List<Vector3>(needlePositions);
        needlePositions.Clear();
        for (int i = 0; i < maxNeedles; ++i)
        {
            Vector3 needlePosition = Vector3.Lerp(
                prevPositions[i],
                needleAnchorCenter + transform.TransformVector(new Vector3(
                    Mathf.Cos(radianIncrement * i) * spreadHorizontal / 2,
                    Mathf.Sin(radianIncrement * i) * spreadVertical / 2, // unsure of why I have to divide by 2 here
                    0
                )),
                followSpeed * Time.deltaTime
            );
            needlePositions.Add(needlePosition);
        }

        // move needles
        for (int i = 0; i < Needles.Count; ++i)
        {
            Needles[i].transform.SetPositionAndRotation(
                needlePositions[i],
                Quaternion.Lerp(Needles[i].transform.rotation, transform.rotation, followSpeed * Time.deltaTime)
            );
        }

        UpdateGrabNeedle();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(threadStartPoint.position, 0.1f);

        if (Application.isPlaying && grabbedNeedle != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(grabbedNeedle.GetComponent<Needle>().needleBack.position, 0.25f);
        }
    }

    // no longer need this function, but keeping it around in case I need a homing needle later!
    private IEnumerator FireNeedleAtTarget(GameObject needle, Vector3 hitPoint, GameObject targetObj = null)
    {
        Vector3 targetPoint = hitPoint;

        while (Vector3.Distance(needle.transform.position, targetPoint) > 0.1f)
        {
            needle.transform.SetPositionAndRotation(
                position: Vector3.MoveTowards(
                    needle.transform.position,
                    targetPoint,
                    fireSpeed * Time.deltaTime
                ),
                rotation: Quaternion.Lerp(
                    needle.transform.rotation,
                    Quaternion.LookRotation(targetPoint - needle.transform.position),
                    fireSpeed * Time.deltaTime
                )
            );

            if (targetObj != null)
            {
                MeshRenderer targetMeshRenderer = targetObj.GetComponent<MeshRenderer>();
                targetPoint = targetMeshRenderer.bounds.center;
            }
            yield return null;
        }

        if (targetObj != null)
        {
            needle.transform.position = targetPoint;
            needle.transform.parent = targetObj.transform;
        }
    }

    public void Fire(InputAction.CallbackContext context)
    {
        if (!context.started || cameraManager.Isometric || Needles.Count == 0 || !canFire)
            return;

        GameObject firedNeedle = Needles[0];
        Needles.RemoveAt(0);
        Vector3 launchPoint = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane))
                                + 2.5f * Camera.main.transform.forward;
        firedNeedle.GetComponent<Needle>().Fire(launchPoint);
    }

    private IEnumerator RetrieveNeedle(GameObject needle)
    {
        needle.transform.parent = null;
        Vector3 targetPoint = needlePositions[Needles.Count];
        float travelTime = 0f;

        // TODO: room for optimization here. A linear scan each loop is less than ideal
        while (Vector3.Distance(needle.transform.position, targetPoint) > 1f && travelTime < 2f && !Needles.Contains(needle))
        {
            travelTime += Time.deltaTime;
            needle.transform.SetPositionAndRotation(
                position: Vector3.Lerp(
                    needle.transform.position,
                    targetPoint,
                    retrievalSpeed * Time.deltaTime
                ),
                rotation: Quaternion.Lerp(
                    needle.transform.rotation,
                    transform.rotation,
                    retrievalSpeed * Time.deltaTime
                )
            );
            yield return new WaitForEndOfFrame();
        }
        Needles.Add(needle);
    }

    public void Grab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!grabbedNeedle)
                return;

            Debug.Log($"Grabbing needle {grabbedNeedle.name}");

            // TODO: Something a little cleaner for this, but this is totally fine for now
            StartCoroutine(RetrieveNeedle(grabbedNeedle));
        }
        else if (context.canceled)
        {
            // TODO: context-based grab release
        }
    }

    private void UpdateAnchors()
    {
        needleAnchorCenter = meshRenderer.bounds.center
            + (meshRenderer.bounds.size.y / 4) * transform.up
            - distanceFromPlayer * transform.forward;
    }

    private void UpdateGrabNeedle()
    {
        Vector3 origin = cameraManager.GetActiveCamera().transform.position;
        Vector3 direction = cameraManager.GetActiveCamera().transform.forward;
        if (Physics.SphereCast(origin, grabRadius, direction, out RaycastHit hit, grabDistance, LayerMask.GetMask("Needle")))
        {
            if (!Needles.Contains(hit.collider.gameObject))
            {
                Debug.Log($"Needle {hit.collider.name} is stuck in a wall and can be freed!");
                grabbedNeedle = hit.collider.gameObject;
            }
        }
        else
        {
            grabbedNeedle = null;
        }
    }
}
