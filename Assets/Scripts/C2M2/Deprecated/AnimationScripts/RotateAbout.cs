using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAbout : MonoBehaviour
{
    public Transform objectToRotate;
    public bool lookAtPoint = true;
    public Vector3 lookAt = Vector3.zero;
    public float speed = 10.0f;
    private void Start()
    {
        if(objectToRotate == null)
        {
            Debug.LogError("In RotateAbout: No transform to rotate given.");
            Destroy(this);
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        objectToRotate.RotateAround(lookAt, transform.up, speed * Time.deltaTime);

        if (lookAtPoint)
        {
            objectToRotate.LookAt(lookAt);
        }
    }
}