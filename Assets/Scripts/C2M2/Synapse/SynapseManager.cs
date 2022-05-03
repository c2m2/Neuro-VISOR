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

    ///<summary>
    ///Simulation refrence to get the attributes of the current cell
    ///</summary>
    public List<Interactable> Simulation
    {
        get
        {
            return GameManager.instance.activeSims;
        }
    }

    public override GameObject IdentifyBuildPrefab(NDSimulation sim, int index)
    {
        if (synapsePrefab == null)
        {
            Debug.LogError("No Synapse prefab found");
            return null;
        }
        else return synapsePrefab;
    }

    /// <summary>
    /// Handles synapse placement
    /// </summary>
    /// <param name="placedSynapse"></param>
    public void SynapticPlacement(Synapse placedSynapse)
    {
        if (synapseInProgress == null) //Pre Synapse
        {
            synapseInProgress = placedSynapse;
        }
        else //Post Synapse
        {
            synapses.Add((synapseInProgress, placedSynapse));
            synapseInProgress = null;
            PlaceArrow();
        }
    }

    public (Synapse, Synapse)? FindSynapsePair(Synapse syn)
    {
        for (int i = 0; i < synapses.Count; i++)
        {
            if (synapses[i].Item1 == syn || synapses[i].Item2 == syn)
            {
                return synapses[i];
            }
        }
        return null;
    }

    public bool DeleteSyn(Synapse syn)
    {
        if (FindSynapsePair(syn) != null)
        {
            (Synapse, Synapse) pair = ((Synapse, Synapse))FindSynapsePair(syn);
            Destroy(pair.Item1.gameObject);
            Destroy(pair.Item2.gameObject);
            synapses.Remove(pair);
            return true;
        }
        else
        {
            return false;
        }
        
    }

    public bool ChangeModel(Synapse syn, Synapse.Model model)
    {

        if (FindSynapsePair(syn) != null)
        {
            (Synapse, Synapse) pair = ((Synapse, Synapse))FindSynapsePair(syn);
            pair.Item1.SwitchModel(model);
            pair.Item2.SwitchModel(model);
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
        if (Simulation == null) return;

        GameObject arrowHead;
        Transform preSynapse = synapses[synapses.Count - 1].Item1.transform;
        Transform postSynapse = synapses[synapses.Count - 1].Item2.transform;

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
                // If there is a synapse on that 1D vertex, the spot is not open
                if (focusVert == index)
                {
                    Debug.LogWarning("Clamp already exists on focus vert [" + index + "]");
                    return false;
                }
                // If there is a synapse within distanceBetweenSynapses, the spot is not open
                else
                {
                    float dist = (sim.Verts1D[focusVert] - sim.Verts1D[index]).magnitude;
                    if (dist < distanceBetweenSynapses)
                    {
                        Debug.LogWarning("Synapse too close to synapse located on vert [" + focusVert + "].");
                        return false;
                    }
                }
            }
            if (syns.Item2.simulation == sim)
            {
                int focusVert = syns.Item2.FocusVert;
                // If there is a synapse on that 1D vertex, the spot is not open
                if (focusVert == index)
                {
                    Debug.LogWarning("Clamp already exists on focus vert [" + index + "]");
                    return false;
                }
                // If there is a synapse within distanceBetweenSynapses, the spot is not open
                else
                {
                    float dist = (sim.Verts1D[focusVert] - sim.Verts1D[index]).magnitude;
                    if (dist < distanceBetweenSynapses)
                    {
                        Debug.LogWarning("Synapse too close to synapse located on vert [" + focusVert + "].");
                        return false;
                    }
                }
            }

        }
        return true;
    }
}
