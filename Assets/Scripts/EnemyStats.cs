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

    public bool pinnable;

    internal EnemyState state;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // TODO: this is just default for now, we can make a better interface for this later
        state = EnemyState.Chasing;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
