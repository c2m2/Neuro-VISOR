using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Use this script for object within the game


public class emulatorMove : MonoBehaviour
{
    private bool isMouseDrag;
    private Vector3 offset;
    private Vector3 screenPosition;
    private GameObject target;
    private GameObject parent;

    //return the object that was ray casted on
    GameObject ReturnClickedObject(out RaycastHit hit)
    {
        GameObject target = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction * 10, out hit))
        {
            try
            {
                parent = hit.collider.transform.parent.gameObject;
            }
            catch(NullReferenceException ex)
            {
                target = hit.collider.gameObject;
                return target;
            }
            if(hit.collider.transform.parent.gameObject != null)
            {
                parent = hit.collider.transform.parent.gameObject;
                return parent;
            }
            
        }
        return target;
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hitInfo;
            target = ReturnClickedObject(out hitInfo);
            if (target != null)
            {
                isMouseDrag = true;
                //Convert world position to screen position.
                screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);
                offset = target.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z));
            }
        }
        //Get right mouse button up
        if (Input.GetMouseButtonUp(1))
        {
            isMouseDrag = false;
        }

        if (isMouseDrag)
        {
            //track mouse position.
            Vector3 currentScreenSpace = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z);

            //convert screen position to world position with offset changes.
            Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenSpace) + offset;

            //It will update target gameobject's current postion.
            //Debug.Log(target.name);
            if(target.gameObject == this.gameObject)
            {
                this.transform.position = currentPosition;
            }
        }

    }
}
