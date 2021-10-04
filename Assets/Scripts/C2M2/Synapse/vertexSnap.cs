using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2;
using C2M2.NeuronalDynamics.UGX;
using C2M2.NeuronalDynamics.Simulation;


public class vertexSnap : MonoBehaviour
{
    public float LatchDistance;
    public Vector3 SynapseLocation;
    List<Vector3> dendriteLocation = new List<Vector3>();
    int count = 0;
    int temp = 0;
    GameObject Synapse;

    //Simulation refrence to get the attributes of the current cell
    public NDSimulation Simulation
    {
        get
        {
            if (GameManager.instance.activeSim != null) return (NDSimulation)GameManager.instance.activeSim;
            else return GetComponentInParent<NDSimulation>();
        }
    }

    //Accessing the end nodes of the neuron
    public List<Neuron.NodeData> Dendrites
    {
        get { return Simulation.Neuron.boundaryNodes; }
    }



    void Start()
    {
        SynapseLocation = this.transform.position;
        Synapse = this.gameObject;
    }


    //Getting the boundary nodes (Dendrites) into a Vector3 List
    public List<Vector3> getBoundaryNodes()
    {
        List<Vector3> getDendriteLocations = new List<Vector3>();
        if (GameManager.instance.activeSim != null)
        {
            for (int i = 0; i < Dendrites.Count; i++)
            {   
                getDendriteLocations.Add(new Vector3((float)Dendrites[i].Xcoords, (float)Dendrites[i].Ycoords, (float)Dendrites[i].Zcoords));
            }

        }
        return getDendriteLocations;
    }


    void Update()
    {
        if (GameManager.instance.activeSim != null)
        {
            
            //Find the dendrites just once per simulation or else this leads to very bad locations
            if (temp == 0)
            {
                dendriteLocation = getBoundaryNodes();
            }
            temp++;

            if (count >= dendriteLocation.Count)
            {
                count = 0;
            }
            //When pressing E increment the count to the next dendrite location
            if (Input.GetKeyDown(KeyCode.E))
            {
                Synapse.transform.SetParent(Simulation.transform);
                Vector3 currentD = dendriteLocation[count];
                Synapse.transform.localPosition = currentD;
                //Debug.Log(currentD.ToString("F4"));
                //Debug.Log(count);
                count++;
            }   
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Synapse.transform.parent = null;
            }
        }

        //If the simulation is ended put back to original location
        if (GameManager.instance.activeSim == null)
        {
            Synapse.transform.parent = null;
            transform.position = Vector3.MoveTowards(transform.position, SynapseLocation, (float).03);
            temp = 0;
        }

    }
}
