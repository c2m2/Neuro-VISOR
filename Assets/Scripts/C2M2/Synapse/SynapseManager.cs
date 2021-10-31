using C2M2;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynapseManager : MonoBehaviour
{
    public GameObject synapse;
    bool activeSynapse = false;

    public NDSimulation Simulation
    {
        get
        {
            if (GameManager.instance.activeSim != null) return (NDSimulation)GameManager.instance.activeSim;
            else return GetComponentInParent<NDSimulation>();
        }
    }

    //This might enable the raycast event for my script, but I have to add a controller maybe still?
    //Sim.raycastEventManager.LRTrigger = GameManager.instance.ndClampManager.hitEvent;

    //void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        RaycastHit hit;
    //        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //        if (Physics.Raycast(ray, out hit, 10.0f))
    //        {
    //            BuildSynapse(hit);
    //        }
    //    }
    //}

    //See raycast press events script to understand how we use raycast
    //public void InstantiateSynapse(RaycastHit hit)
    //{
    //    Debug.Log("RayCast");
    //    BuildSynapse(hit);
    //}


    //private Synapse BuildSynapse(RaycastHit hit)
    //{

    //    if (PreSynapse == null || PostSynapse == null)
    //    {
    //        Debug.LogError("PreSynapse or PostSynapse prefab not found!");
    //    }

    //    // If there is no NDSimulation, don't try instantiating a clamp
    //    if (hit.collider.GetComponentInParent<NDSimulation>() == null) return null;

    //    // Find the 1D vertex that we hit
    //    int synapseIndex = Simulation.GetNearestPoint(hit);

    //    Synapse synapse;

    //    synapse = Instantiate(PreSynapse, Simulation.transform).GetComponentInChildren<Synapse>();
    //    synapse.AttachSimulationSynapse(Simulation, synapseIndex);

    //    return synapse;
    //}


    void Update()
    {
        //Method
        //GameObject syn = GameObject.Find("SynapseTEST");
        //List<Vector3> temp = new List<Vector3>();
        //temp = syn.GetComponent<vertexSnap>().synapseLocations;
        //Method


        //If there is a current simulation and we press E then we can create a synapse
        if (Input.GetKeyDown(KeyCode.E) && GameManager.instance.activeSim != null && activeSynapse == false)
        {
            synapse.SetActive(true);
            activeSynapse = true;
        }

        //If we press E again set the synapse to false so we can no longer use it 
        else if(Input.GetKeyDown(KeyCode.E) && GameManager.instance.activeSim != null && activeSynapse == true)
        {
            synapse.SetActive(false);
            activeSynapse = false;
        }

        //If there is no current simulation set the synapse to not active
        if(GameManager.instance.activeSim == null)
        {
            synapse.SetActive(false);
            activeSynapse = false;
        }
    }
}
