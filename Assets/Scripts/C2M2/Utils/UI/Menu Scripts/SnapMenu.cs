using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapMenu : MonoBehaviour
{
    public Transform playerHolder;
    // Start is called before the first frame update
    void Start()
    {
      //  transform.position = playerHolder.position;
      //  transform.rotation = playerHolder.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            transform.position = playerHolder.position;
            transform.eulerAngles = new Vector3(playerHolder.eulerAngles.x, playerHolder.eulerAngles.y, 0f);
        }
    }
}
