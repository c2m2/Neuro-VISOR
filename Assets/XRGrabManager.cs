using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XRGrabManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(gameObject.transform.childCount>1)
        {
            StartCoroutine(CoroutineXRGrab());
        }
    }

    IEnumerator CoroutineXRGrab()
    {
        yield return new WaitForSeconds(3);
        gameObject.AddComponent<XRGrabInteractable>();
    }
}
