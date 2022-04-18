using System.Collections.Generic;
using UnityEngine;
using C2M2;
using C2M2.Interaction;
using C2M2.Simulation;
using C2M2.NeuronalDynamics.Simulation;

public class SynapseManager : NDInteractablesManager<Synapse>
{
    public List<Vector3> SynapticNodeLocation = new List<Vector3>();
    public GameObject PrefabPreSynapse;
    public GameObject PrefabPostSynapse;
    private Synapse synapseInProgress = null; //Contains presynapse when a presynapse has been placed but no post synapse
    public List<(Synapse, Synapse)> synapses = new List<(Synapse, Synapse)>(); //pre (Item1) and post (Item2) synapses
    public List<Vector3> synapseLocations = new List<Vector3>();
    public RaycastPressEvents HitEvent { get; private set; } = null;

    public GameObject arrow;
    public int focusVert { get; private set; } = -1;


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

    /// <summary>
    /// Handles synapse placement
    /// </summary>
    /// <param name="hit"></param>
    void SynapticPlacement(RaycastHit hit)
    {

        // from our raycast hit get the 1d node that we raycasted onto
        int synapticIndex = currentSimulation.GetNearestPoint(hit);

        // check all other synapse node indices and make sure there are not any others there
        for (int i = 0; i < synapses.Count; i++)
        {
            if (synapses[i].Item1.nodeIndex == synapticIndex || synapses[i].Item2.nodeIndex == synapticIndex)
            {
                Debug.LogWarning("Can not place two synapses on top of each other");
                return;
            }
        }

        if (!synapseInProgress)
        {
            PreSynapticPlacement(hit, currentSimulation, synapticIndex);
        }
        else
        {
            PostSynapticPlacement(hit, currentSimulation, synapticIndex);
        }

        holdCount = 0;
    }

    /// <summary>
    /// Placement and instantiation for our pre-Synapse.
    /// This is the same as postsynaptic </summary>
    /// <param name="hit"></param>
    /// <param name="curSimulation"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private void PreSynapticPlacement(RaycastHit hit, NDSimulation curSimulation, int index)
    {
        Synapse pre = gameObject.AddComponent<Synapse>();
        pre.prefab = Instantiate(PrefabPreSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        pre.nodeIndex = index;
        pre.attachedSim = curSimulation;
        pre.transformRayCast(hit);
        synapseLocations.Add(pre.prefab.transform.localPosition);

        synapseInProgress = pre;
    }


    /// <summary>
    /// Placement and instantiation for our post-Synapse.
    /// This is the same as presynaptic </summary>
    /// <param name="hit"></param>
    /// <param name="curSimulation"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private void PostSynapticPlacement(RaycastHit hit, NDSimulation curSimulation, int index)
    {
        Synapse post = gameObject.AddComponent<Synapse>();
        post.prefab = Instantiate(PrefabPostSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
        post.nodeIndex = index;
        post.attachedSim = curSimulation;
        post.transformRayCast(hit);
        synapseLocations.Add(post.prefab.transform.localPosition);

        synapses.Add((synapseInProgress, post));
        synapseInProgress = null;

        PlaceArrow();
    }

    /// <summary>
    /// If a user holds a raycast onto the synapse for x frames delete the synapse
    /// </summary>
    /// <param name="hit"></param>
    void DeleteSynapseHit(RaycastHit hit) //TODO probably could just see if being raycasted on the object like clamps
    {
        NDSimulation curSimulation = hit.collider.GetComponentInParent<NDSimulation>();

        holdCount++;
        // Hold count threshhold to check if the user has pressed for x frames
        if (holdCount >= DestroyCount)
        {
            // Get the 1d vertex user has pressed
            int hitIndex = curSimulation.GetNearestPoint(hit);

            for (int i = 0; i < synapses.Count; i++)
            {
                // if user has pressed onto the pre-synapse
                if (synapses[i].Item1.nodeIndex == hitIndex || synapses[i].Item2.nodeIndex == hitIndex)
                {
                    // delete and remove from synapse list
                    Destroy(synapses[i].Item1);
                    Destroy(synapses[i].Item1.prefab);
                    Destroy(synapses[i].Item2);
                    Destroy(synapses[i].Item2.prefab);
                    synapses.RemoveAt(i);
                    holdCount = 0;
                    return;
                }
            }
            holdCount = 0;
        }
    }
    
    
    /// <summary>
    /// When this script has been enabled through the game object it is attached to initialize some values
    /// </summary>
    private void OnEnable()
    {
        // Trigger events used for raycasting to the neuron

        /* hitEvent is a refrence to the RaycastPressEvents script.
         * Which allows us to use predefined ray casting methods*/
        HitEvent = gameObject.AddComponent<RaycastPressEvents>();

        // Switch default raycasting mode to our new hit event
        //Simulations.raycastEventManager.LRTrigger = this.hitEvent;

        HitEvent.OnPress.AddListener((hit) => SynapticPlacement(hit));
        HitEvent.OnHoldPress.AddListener((hit) => DeleteSynapseHit(hit));
    }
    
    /// <summary>
    /// When the script has been disabled try passing our synapse information to the correct simulations
    /// </summary>
    private void OnDisable()
    {
        // This prevents adding many RaycastPressEvents scripts each time user enables() this script
        Destroy(GetComponent<RaycastPressEvents>());
    }
    
    /// <summary>
    /// This creates an arrow prefab that points the pre-synapse to the post-synapse
    /// </summary>
    private void PlaceArrow()
    {
        if (Simulation == null) return;

        GameObject arrowHead;
        Transform preSynapse = synapses[synapses.Count - 1].Item1.prefab.transform;
        Transform postSynapse = synapses[synapses.Count - 1].Item2.prefab.transform;

        // Create a new arrow in 3D space
        arrowHead = Instantiate(arrow, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
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
    override public bool VertexAvailable(int index)
    {
        // minimum distance between synapses
        float distanceBetweenSynapses = currentSimulation.AverageDendriteRadius * 2;

        foreach ((Synapse,Synapse) syns in synapses)
        {
            if (syns.Item1.simulation == currentSimulation)
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
                    float dist = (currentSimulation.Verts1D[focusVert] - currentSimulation.Verts1D[index]).magnitude;
                    if (dist < distanceBetweenSynapses)
                    {
                        Debug.LogWarning("Synapse too close to synapse located on vert [" + focusVert + "].");
                        return false;
                    }
                }
            }
            if (syns.Item2.simulation == currentSimulation)
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
                    float dist = (currentSimulation.Verts1D[focusVert] - currentSimulation.Verts1D[index]).magnitude;
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

        //TODO prefab?

}
