using UnityEngine;

public class BreakableWindow : MonoBehaviour, IPullable
{
    [SerializeField]
    float breakForce;

    private bool isBroken;

    // Start is called before the first frame update
    void Start()
    {
        isBroken = false;
    }

    void Break()
    {
        if (isBroken)
            return;

        foreach (Transform child in transform.Find("StuffInWindow"))
        {
            child.GetComponent<Collider>().enabled = true;

            Rigidbody rb = child.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(breakForce * (child.transform.position - transform.Find("BreakAnglePoint").position), ForceMode.Impulse);
        }

        GetComponent<Collider>().enabled = false;
    }

    public void OnNeedlePulled()
    {
        Break();
    }
}
