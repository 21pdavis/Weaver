using UnityEngine;

public class CompanionMovement : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The Transform near the player to follow.")]
    private Transform playerFollowPoint;

    [SerializeField]
    private float followSpeed;

    [SerializeField]
    [Tooltip("How much the companion rotates at each tick around the follow point.")]
    private float orbitDegreesPerSecond;

    [SerializeField]
    private float orbitRadius;

    private float pointOnUnitCircle;

    // Start is called before the first frame update
    void Start()
    {
        pointOnUnitCircle = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        pointOnUnitCircle += Mathf.Deg2Rad * orbitDegreesPerSecond * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(pointOnUnitCircle), Mathf.Sin(pointOnUnitCircle), 0f);

        // Undo the scaling of the playerFollowPoint
        offset.x /= playerFollowPoint.lossyScale.x;
        offset.y /= playerFollowPoint.lossyScale.y;
        offset.z /= playerFollowPoint.lossyScale.z;

        offset *= orbitRadius;

        transform.position = Vector3.Lerp(
            a: transform.position,
            b: playerFollowPoint.TransformPoint(offset),
            t: followSpeed * Time.deltaTime
        );
    }
}
