using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPivot : MonoBehaviour
{
    [SerializeField]
    private Transform cam;

    // Update is called once per frame
    void Update()
    {
        transform.SetPositionAndRotation(
            cam.position,
            Quaternion.Euler(0, cam.rotation.eulerAngles.y, 0)
        );
    }
}
