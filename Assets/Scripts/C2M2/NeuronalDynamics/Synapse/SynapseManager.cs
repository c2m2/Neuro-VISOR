using System.Collections.Generic;
using UnityEngine;
using C2M2;
using C2M2.Simulation;
using C2M2.NeuronalDynamics.Simulation;

public class SynapseManager : NDInteractablesManager<Synapse>
{
    public GameObject synapsePrefab;
    public GameObject arrowPrefab;
    private Synapse synapseInProgress = null; //Contains presynapse when a presynapse has been placed but no post synapse
    public List<(Synapse, Synapse)> synapses = new List<(Synapse, Synapse)>(); //pre (Item1) and post (Item2) synapses

    public override GameObject IdentifyBuildPrefab(NDSimulation sim, int index)
    {
        if (synapsePrefab == null)
        {
            Debug.LogError("No Synapse prefab found");
            return null;
        }
        else return synapsePrefab;
    }

    private void OnDestroy()
    {
        foreach ((Synapse, Synapse) synapsePair in synapses)
        {
            Destroy(synapsePair.Item1);
            Destroy(synapsePair.Item2);
        }
        synapses.Clear();
    }
    
    // Returns the synapse object corresponding to the currently selected synapse 
    public  Synapse FindSelectedSyn(Synapse syn)
    {
        for (int i=0; i<synapses.Count; i++)
        {
            if (synapses[i].Item1.FocusVert == syn.FocusVert && synapses[i].Item1.simulation.Neuron == syn.simulation.Neuron)
            {
                return synapses[i].Item1;
            }
            else if (synapses[i].Item2.FocusVert == syn.FocusVert && synapses[i].Item2.simulation.Neuron == syn.simulation.Neuron)
            {
                return synapses[i].Item2;
            }
        }
        return null;
    }

    /// <summary>
    /// Handles synapse placement
    /// </summary>
    /// <param name="placedSynapse"></param>
    public void SynapticPlacement(Synapse placedSynapse)
    {
        if (synapseInProgress == null) //Pre Synapse
        {
            Synapse prePlaced = placedSynapse.Clone();
            synapseInProgress = prePlaced;
        }
        else //Post Synapse
        {
            Synapse postPlaced = placedSynapse.Clone();
            synapses.Add((synapseInProgress, postPlaced));
            synapseInProgress = null;
            PlaceArrow();
        }
    }

    public List<(Synapse, Synapse)> FindSynapsePair(Synapse syn)
    {
        List<(Synapse, Synapse)> syns = new List<(Synapse, Synapse)>();
        if (synapses.Count != 0) 
        {
            for (int i = 0; i < synapses.Count; i++)
            {
                if (synapses[i].Item1 == syn || synapses[i].Item2 == syn)
                {
                    syns.Add((synapses[i].Item1, synapses[i].Item2));
                }
            }
            return syns;
        }
        else return null;
    }

    public bool DeleteSyn(Synapse syn)
    {
        if (FindSynapsePair(syn) != null)
        {
            foreach ((Synapse, Synapse) pair in FindSynapsePair(syn))
            {
                Destroy(pair.Item1.gameObject);
                Destroy(pair.Item2.gameObject);
                synapses.Remove(pair);
            }
            return true;
        }
        else return false;
    }

    public bool ChangeModel(Synapse syn, Synapse.Model model)
    {
        if (FindSynapsePair(syn) != null)
        {
            foreach ((Synapse, Synapse) pair in (FindSynapsePair(syn)))
            {
                pair.Item1.SwitchModel(model);
                pair.Item2.SwitchModel(model);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// This creates an arrow prefab that points the pre-synapse to the post-synapse
    /// </summary>
    private void PlaceArrow()
    {
        GameObject arrowHead;
        Transform preSynapse = synapses[synapses.Count - 1].Item1.transform;
        Transform postSynapse = synapses[synapses.Count - 1].Item2.transform;
        
        Synapse pre = synapses[synapses.Count - 1].Item1;
        Synapse post = synapses[synapses.Count - 1].Item2;

        // Create a new arrow in 3D space
        arrowHead = Instantiate(arrowPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        /* Use Vector3 lerp so the position does not set it to the middle of the pre synapse but rather in the middle of both the pre-synapse and post-synapse*/
        arrowHead.transform.position = Vector3.Lerp(preSynapse.position, postSynapse.position, 0.5f);
        arrowHead.transform.LookAt(postSynapse.position);
        // Adjust the z scale of the arrow so we can point correctly to the post-synapse
        // We can calculate this by the distance of the two synapses
        arrowHead.transform.localScale = new Vector3(preSynapse.lossyScale.x / 4, preSynapse.lossyScale.x / 4, Vector3.Distance(preSynapse.position, postSynapse.position));
        arrowHead.transform.SetParent(preSynapse);

        // Add the method to update arrows when user moves the neurons
        arrowHead.AddComponent<ArrowUpdate>();
        arrowHead.GetComponent<ArrowUpdate>().preSynapse = preSynapse;
        arrowHead.GetComponent<ArrowUpdate>().postSynapse = postSynapse;
        
        // Assign current synapses to fields of ArrowUpdate to ensure color changes with synapse model
        arrowHead.GetComponent<ArrowUpdate>().pre = pre;
        arrowHead.GetComponent<ArrowUpdate>().post = post;
    }

    /// <summary>
    /// Ensures that no synapse is placed too near to another synapse
    /// </summary>
    override public bool VertexAvailable(NDSimulation sim, int index)
    {
        // minimum distance between synapses
        float distanceBetweenSynapses = sim.AverageDendriteRadius * 2;
        foreach ((Synapse,Synapse) syns in synapses)
        {
            if (syns.Item1.simulation == sim)
            {
                int focusVert = syns.Item1.FocusVert;
                float dist = (sim.Verts1D[focusVert] - sim.Verts1D[index]).magnitude;
                if (dist < distanceBetweenSynapses)
                {
                    Debug.LogWarning("Synapse too close to synapse located on vert [" + focusVert + "].");
                    return false;
                }
            }
            if (syns.Item2.simulation == sim)
            {
                int focusVert = syns.Item2.FocusVert;
                float dist = (sim.Verts1D[focusVert] - sim.Verts1D[index]).magnitude;
                if (dist < distanceBetweenSynapses)
                {
                    Debug.LogWarning("Synapse too close to synapse located on vert [" + focusVert + "].");
                    return false;
                }
            }
        }
        return true;
    }
}
