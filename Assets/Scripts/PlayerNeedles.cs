using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to manage and track the player's needles
/// </summary>
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(needleAnchorCenter, 0.1f);
        Gizmos.DrawSphere(needleAnchorCenter + spreadVertical * Vector3.up, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(needleAnchorLeft, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(needleAnchorRight, 0.1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

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
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAnchors();

        // TODO: handle changing maxNeedles
        // determine next needle positions
        List<Vector3> prevPositions = new List<Vector3>(needlePositions);
        needlePositions.Clear();
        for (int i = 0; i < maxNeedles; ++i)
        {
            needlePositions.Add(Vector3.Lerp(
                prevPositions[i],
                needleAnchorCenter + transform.TransformVector(new Vector3(
                    Mathf.Cos(radianIncrement * i) * spreadHorizontal / 2,
                    Mathf.Sin(radianIncrement * i) * spreadVertical / 2, // unsure of why I have to divide by 2 here
                    0
                )),
                followSpeed * Time.deltaTime
            ));
        }

        // move needles
        for (int i = 0; i < maxNeedles; ++i)
        {
            Needles[i].transform.position = needlePositions[i];
            Needles[i].transform.rotation = Quaternion.Lerp(Needles[i].transform.rotation, transform.rotation, followSpeed * Time.deltaTime);
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
