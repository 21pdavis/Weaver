using UnityEngine;

// TODO: is stats the best name for this? I almost want to split into stats and "controller" or something like that for state management
public class EnemyStats : MonoBehaviour
{
    internal enum EnemyState
    {
        //Idle,
        //Attacking,
        //Stunned,
        //Dead,
        //Suspended,
        Chasing,
        Pinned
    }

    internal EnemyState state {
        get { return state; }
        set
        {
            // TODO: not actually thrilled with the separation here, but we'll see
            switch (value)
            {
                case EnemyState.Chasing:
                    break;
                case EnemyState.Pinned:
                    enemyMovement.navMeshAgent.enabled = false;
                    enemyMovement.GetComponentInChildren<Collider>().enabled = false;
                    transform.localPosition = Vector3.zero;
                    break;
            }
        }
    }

    public bool pinnable;

    private EnemyMovement enemyMovement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // TODO: this is just default for now, we can make a better interface for this later
        state = EnemyState.Chasing;
        enemyMovement = GetComponent<EnemyMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
