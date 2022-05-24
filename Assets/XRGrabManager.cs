using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// This file is used to allow the 3D cell structure to be grabbable in the XR framework.
// The Unity provided "XRGrabInteractable.cs" does not provide a setter method for the colliders,
// making it impossible to mannualy assign colliders to the GameObjects with XRGrabInteractable component
// attached to it. The original code design contains the "PublicOVRGrabbable.cs" which automatically pick up
// all the colliders and all XRGrabInteractable components in the same hierarchy or above would also get references to all
// the colliders in PublicOVRGrabbable.
// The issue here is the that PublicOVRGrabbable.cs and XRGrabInteractable can not coexist, otherwise the grabbing feature
// in XR would not work. But we still need the intially colliders assignment from the PublicOVRGrabbable.cs.
// Naive Solution: wait for the child object (the 3D cell object with PublicOVRGrabbable.cs to get all the colliders) and assign
// the XRGrabInteractable.cs to the SimulationSpace after the child cell object finished loading, then delete the PublicOVRGrabbable.cs
// from the child cell object. This allows both the colliders to be picked up by the XRGrabInteractable.cs and avoid the incompatibility
// between the two grabbing scripts.

public class XRGrabManager : MonoBehaviour
{
    private bool xrGrabAttached = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.transform.childCount > 1 && !xrGrabAttached)
        {
            StartCoroutine(CoroutineXRGrab());
        }
    }

    IEnumerator CoroutineXRGrab()
    {
        yield return new WaitForSeconds(3);
        Destroy(gameObject.transform.GetChild(1).gameObject.GetComponent("PublicOVRGrabbable"));
        gameObject.AddComponent<XRGrabInteractable>();
        xrGrabAttached = true;

    }
}
