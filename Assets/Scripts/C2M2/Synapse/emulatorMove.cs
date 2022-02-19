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

    // Me
    private bool selected = false;

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
                return parent;
            }
            catch (NullReferenceException)
            {
                target = hit.collider.gameObject;
                return target;
            }
        }
        return target;
    }
    
    // Me
    private List<NDSimulation> selectedList = new List<NDSimulation>();
    void Update()
    {
        // Me
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            target = ReturnClickedObject(out hit);
            try
            {
                NDSimulation sim = target.GetComponent<NDSimulation>();
                sim.selected = true;
                selected = true;
                sim.Select();
                selectedList.Add(sim);
            }
            catch (Exception e) { }
        }

        if (Input.GetKey(KeyCode.C))
        {
            if (selected)
            {
                foreach (NDSimulation s in selectedList)
                {
                    s.StopSelect();
                    s.selected = false;
                }

                selectedList.RemoveRange(0, selectedList.Count);
                selected = false;
            }
        }

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

                // Me
                foreach (NDSimulation s in selectedList)
                {
                    s.distance = s.transform.position - target.transform.position;
                }
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
                NDSimulation sim = this.transform.GetComponent<NDSimulation>();

                this.transform.position = currentPosition;

                if (sim.selected)
                {
                    foreach (NDSimulation s in selectedList)
                    {
                        if (s != this)
                            s.transform.position = currentPosition + s.distance;
                    }
                }
            }
        }

    }
}
