using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class to manage and track the player's needles
/// </summary>
public class PlayerNeedles : MonoBehaviour
{
    [Header("Needle Options")]
    [SerializeField]
    private int startNeedles;

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

    [Header("References")]
    [SerializeField]
    private GameObject needlePrefab;

    public List<GameObject> Needles { get; set; }

    private MeshRenderer meshRenderer;

    private Vector3 needleAnchorCenter;
    private Vector3 needleAnchorLeft;
    private Vector3 needleAnchorRight;
    private List<Vector3> needlePositions;
    private float radianIncrement;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        UpdateAnchors();

        // initial needle positions
        needlePositions = new List<Vector3>();
        radianIncrement = Mathf.Deg2Rad * (180f / (startNeedles - 1));
        for (int i = 0; i < startNeedles; ++i)
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
        for (int i = 0; i < startNeedles; ++i)
        {
            GameObject needle = Instantiate(needlePrefab, needlePositions[i], transform.rotation);
            Needles.Add(needle);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnchors();

        // determine next needle positions
        List<Vector3> prevPositions = new List<Vector3>(needlePositions);
        needlePositions.Clear();
        for (int i = 0; i < startNeedles; ++i)
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
    }

    private IEnumerator FireNeedleAtTarget(GameObject needle, Vector3 hitPoint, GameObject targetObj=null)
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
                //targetPoint = (hitPoint + targetMeshRenderer.bounds.center) * 0.5f;
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
        if (!context.started)
            return;

        // TODO: minimum radius around player to fire
        GameObject firedNeedle = Needles[0];
        Needles.RemoveAt(0);
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            StartCoroutine(FireNeedleAtTarget(firedNeedle, hit.point, hitObject.CompareTag("Enemy") ? hitObject : null));
        }
        else
        {
            Debug.LogWarning("No hit point found for needle");
        }
    }

    private void UpdateAnchors()
    {
        needleAnchorCenter = meshRenderer.bounds.center
            + (meshRenderer.bounds.size.y / 4) * transform.up
            - distanceFromPlayer * transform.forward;

        needleAnchorLeft = needleAnchorCenter - (spreadHorizontal / 2) * transform.right;
        needleAnchorRight = needleAnchorCenter + (spreadHorizontal / 2) * transform.right;
    }
}
