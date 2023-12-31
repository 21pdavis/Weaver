using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField]
    private float distanceFromPlayer;

    private Transform target;
    public NavMeshAgent navMeshAgent { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (navMeshAgent.enabled)
        {
           navMeshAgent.SetDestination(
               Vector3.Distance(target.transform.position, transform.position) > distanceFromPlayer
               ?
               target.transform.position
               :
               target.transform.position + distanceFromPlayer * (transform.position - target.transform.position).normalized
           );
        }
    }
}
