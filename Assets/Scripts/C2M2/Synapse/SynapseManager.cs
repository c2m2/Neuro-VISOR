using C2M2;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynapseManager : MonoBehaviour
{
    public GameObject synapse;
    bool activeSynapse = false;
    public RaycastPressEvents hitEvent { get; private set; } = null;
    public List<Synapse> synapsesList = new List<Synapse>();


    public NDSimulation Simulation
    {
        get
        {
            if (GameManager.instance.activeSim != null) return (NDSimulation)GameManager.instance.activeSim;
            else return GetComponentInParent<NDSimulation>();
        }
    }

    // method to gather the voltage at the presynapse locations
    public void getPreSynapsesVoltage()
    {
       if(synapsesList.Count > 0)
        {
            for (int i = 0; i < synapsesList.Count; i++)
            {
                if (i % 2 == 0)
                {
                    double[] curVoltage = Simulation.Get1DValues();
                    synapsesList[i].voltage = curVoltage[synapsesList[i].nodeIndex];
                }
            }
            setPost1DValues();
        }
    }

    // after receiving the pre-synaptic voltage apply that to the post-synapse
    public void setPost1DValues()
    {
        Tuple<int, double>[] new1Dvalues = new Tuple<int, double>[synapsesList.Count / 2];
        List<Synapse> postSynapse = new List<Synapse>();
        List<Synapse> preSynapse = new List<Synapse>();

        for (int i = 0; i < synapsesList.Count; i++)
        {
            if (i % 2 != 0)
            {
                postSynapse.Add(synapsesList[i]);
            }
            else
            {
                preSynapse.Add(synapsesList[i]);
            }
        }

        for (int i = 0; i < postSynapse.Count; i++)
        {
            new1Dvalues[i] = new Tuple<int, double>(postSynapse[i].nodeIndex, preSynapse[i].voltage);
        }

        Simulation.Set1DValues(new1Dvalues);
    }

    void Update()
    {
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

            // when the synapse script is turned off revert the controls back to defualt
            Simulation.raycastEventManager.LRTrigger = Simulation.defaultRaycastEvent;
        }

        if(GameManager.instance.activeSim != null)
        {
            getPreSynapsesVoltage();
        }

        //If there is no current simulation set the synapse to not active
        else if(GameManager.instance.activeSim == null)
        {
            synapse.SetActive(false);
            activeSynapse = false;
            synapse.GetComponent<vertexSnap>().synapses = new List<Synapse>();
            synapsesList = synapse.GetComponent<vertexSnap>().synapses;
        }
    }
}
