using UnityEngine;
using UnityEngine.InputSystem;

public class CompanionPowers : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // refactor to selecting a power?
    public void Suspend(InputAction.CallbackContext context)
    {
        if (!context.started)
            return;

        // raycast from mouse to world, get closest enemy

        // if enemy is close enough, suspend them (should this be a method in EnemyMovement?)
        // TODO
        //GameObject.Find("Enemy (1)").GetComponent<EnemyMovement>().navMeshAgent.isStopped = true;
    }
}
