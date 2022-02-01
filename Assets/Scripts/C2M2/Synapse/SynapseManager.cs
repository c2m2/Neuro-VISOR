using System.Collections.Generic;
using UnityEngine;
using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;
using System;
using C2M2.Simulation;

public class SynapseManager : MonoBehaviour
{
    public List<Vector3> SynapticNodeLocation = new List<Vector3>();
    public GameObject PrefabPreSynapse;
    public GameObject PrefabPostSynapse;
    private int count = 0;
    public List<Synapse> synapses = new List<Synapse>();
    public List<Vector3> synapseLocations = new List<Vector3>();
    public RaycastPressEvents hitEvent { get; private set; } = null;

    public GameObject arrow;
    private int holdCount = 0;
    public int focusVert { get; private set; } = -1;
    public int numOfDeletionFrames = 50;


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

    public NDSimulation curSimulation = null;

    /// <summary>
    /// Initial placement for the pre-synapse then calls post-synapse placement next
    /// </summary>
    /// <param name="hit"></param>
    void preSynapticPlacement(RaycastHit hit)
    {
        // Get the current simulation we ray cast onto
        curSimulation = hit.collider.GetComponentInParent<NDSimulation>();

        // count = the number of active synapses which is always 0 or 1
        // 0 meaning we can place the pre-synapse and 1 meaning we must place the post-synapse
        if(count == 0)
        {
            // from our raycast hit get the 1d node that we raycasted onto
            int preSynapticIndex = curSimulation.GetNearestPoint(hit);

            focusVert = preSynapticIndex;

            // check all other synapse node index's and make sure we can place them on top of each other
            for (int i = 0; i < synapses.Count; i++)
            {
                if(synapses[i].nodeIndex == preSynapticIndex)
                {
                    return;
                }
            }

            // Use Synapse class to set each instance of the synapse with its own variables
            Synapse pre = gameObject.AddComponent<Synapse>();

            pre.prefab = Instantiate(PrefabPreSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            pre.nodeIndex = preSynapticIndex;

            pre.attachedSim = curSimulation;

            // Add each synapes to our list that is used else where
            synapses.Add(pre);

            // call method to place our synapse prefab on the neuron
            pre.transformRayCast(hit);
            synapseLocations.Add(pre.prefab.transform.localPosition);

            holdCount = 0;
            count++;
            
        }
        //if we have already placed the PreSynaptic synapse place the post instead
        else if(count == 1)
        {
            postSynapticPlacement(hit);
        }
    }

    /// <summary>
    /// Placement and instantiation for our post-Synapse.
    /// This is the same as presynaptic </summary>
    /// <param name="hit"></param>
    void postSynapticPlacement(RaycastHit hit)
    {
        curSimulation = hit.collider.GetComponentInParent<NDSimulation>();
        if(count == 1)
        {
            Synapse post = gameObject.AddComponent<Synapse>();

            post.prefab = Instantiate(PrefabPostSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            int postSynapticIndex = curSimulation.GetNearestPoint(hit);

            focusVert = postSynapticIndex;

            post.nodeIndex = postSynapticIndex;

            post.attachedSim = curSimulation;

            synapses.Add(post);

            post.transformRayCast(hit);
            synapseLocations.Add(post.prefab.transform.localPosition);

            count++;
            placeArrow();
        } 
    }

    /// <summary>
    /// If a user holds a raycast onto the synapse for x frames delete the synapse
    /// </summary>
    /// <param name="hit"></param>
    void deleteSynapseHit(RaycastHit hit)
    {
        curSimulation = hit.collider.GetComponentInParent<NDSimulation>();

        holdCount++;
        // Hold count threshhold to check if the user has pressed for x frames
        if(holdCount >= numOfDeletionFrames)
        {
            // Get the 1d vertex user has pressed
            int hitIndex = curSimulation.GetNearestPoint(hit);

            for(int i = 0; i < synapses.Count; i++)
            {
                // if user has pressed onto the pre-synapse
                if(synapses[i].nodeIndex == hitIndex && i % 2 == 0)
                {    
                    // delete and remove from synapse list
                    Destroy(synapses[i]);
                    Destroy(synapses[i].prefab);
                    Destroy(synapses[i + 1]);
                    Destroy(synapses[i + 1].prefab);
                    synapses.RemoveAt(i);
                    synapses.RemoveAt(i);
                    holdCount = 0;
                    return;
                }
                // else the user has pressed onto the post-synapse
                else if(synapses[i].nodeIndex == hitIndex && i % 2 != 0)
                {
                    Destroy(synapses[i]);
                    Destroy(synapses[i].prefab);
                    Destroy(synapses[i - 1]);
                    Destroy(synapses[i - 1].prefab);
                    synapses.RemoveAt(i);
                    synapses.RemoveAt(i - 1);
                    holdCount = 0;
                    return;
                }
            }
            holdCount = 0;
        }
    }


    /// <summary>
    /// When this script has been enabled through the game object it is attached to initiliaze some values
    /// </summary>
    private void OnEnable()
    {
        // Trigger events used for raycasting to the neuron

        /* hitEvent is a refrence to the RaycastPressEvents script.
         Which allows us to use predefined ray casting methods*/
        hitEvent = gameObject.AddComponent<RaycastPressEvents>();

        // Switch default raycasting mode to our new hit event
        //Simulations.raycastEventManager.LRTrigger = this.hitEvent;

        hitEvent.OnPress.AddListener((hit) => preSynapticPlacement(hit));
        hitEvent.OnHoldPress.AddListener((hit) => deleteSynapseHit(hit));
    }

    /// <summary>
    /// When the script has been disabled try passing our synapse information to the correct simulations
    /// </summary>
    private void OnDisable()
    {
        // Pass refrence of our updated synapse list to the NDSimulation

        try
        {
            // Check if each postsynaptic is attached to a unique simulation, if it is add the post and pre to that simulation
            for(int i = 0; i < Simulation.Count; i++)
            {
                for(int j = 0; j < synapses.Count; j++)
                {
                    if(Simulation[i].GetComponent<NDSimulation>() == synapses[j].attachedSim)
                    {
                        if(j % 2 != 0)
                        {
                            //presynaptic 
                            Simulation[i].GetComponent<NDSimulation>().synapses.Add(synapses[j - 1]);
                            // postsynaptic
                            Simulation[i].GetComponent<NDSimulation>().synapses.Add(synapses[j]);
                        }
                    }
                }
            }
        }
        catch (NullReferenceException)
        {
            Debug.Log("Synapses Disabled");
        }
        catch (MissingReferenceException)
        {
            Debug.Log("Synapses Disabled");
        }

        // This prevents adding many RaycastPressEvents scripts each time user enables() this script
        Destroy(GetComponent<RaycastPressEvents>());
    }


    /// <summary>
    /// This creates an arrow prefab that points the pre-synapse to the post-synapse
    /// </summary>
    void placeArrow()
    {
        if (Simulation != null)
        {
            // Only true if we have placed the post-synapse
            if (count == 2)
            {
                for (int i = synapses.Count - 2; i < synapses.Count - 1; i++)
                {
                    // for each pre-synapse attach an arrow gameobject
                    if (i % 2 == 0)
                    {
                        GameObject arrowHead;
                        Transform preSynapse = synapses[i].prefab.transform;
                        Transform postSynapse = synapses[i + 1].prefab.transform;

                        // Create a new arrow in 3D space
                        arrowHead = Instantiate(arrow, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                        /* Use Vector3 lerp so the position does not set it to the middle of the pre synapse but rather in the middle
                        of both the pre-synapse and post-synapse*/
                        arrowHead.transform.position = Vector3.Lerp(preSynapse.position, postSynapse.position, 0.5f);
                        arrowHead.transform.LookAt(postSynapse.position);

                        // Adjust the z scale of the arrow so we can point correctly to the post-synapse
                        // We can calculate this by the distance of the two synapses
                        arrowHead.transform.localScale = new Vector3(preSynapse.lossyScale.x / 4, preSynapse.lossyScale.x / 4, Vector3.Distance(preSynapse.position, postSynapse.position));
                        arrowHead.transform.SetParent(preSynapse);
                    }
                }
                count = 0;
            }
        }
    }

}
