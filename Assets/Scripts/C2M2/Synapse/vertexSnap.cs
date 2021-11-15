using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2;
using C2M2.NeuronalDynamics.UGX;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;
using System;

public class vertexSnap : MonoBehaviour
{
    public List<Vector3> SynapticNodeLocation = new List<Vector3>();
    public GameObject PrefabPreSynapse;
    public GameObject PrefabPostSynapse;
    private int count = 0;
    public List<Synapse> synapses = new List<Synapse>();
    public Material material;
    public List<Vector3> synapseLocations = new List<Vector3>();
    public RaycastPressEvents hitEvent { get; private set; } = null;
    public SynapseManager SynapseManager;
    public GameObject arrow;
    private int holdCount = 0;

    //Simulation refrence to get the attributes of the current cell
    public NDSimulation Simulation
    {
        get
        {
            if (GameManager.instance.activeSim != null) return (NDSimulation)GameManager.instance.activeSim;
            else return GetComponentInParent<NDSimulation>();
        }
    }

    //Accessing the node data of the neuron
    public List<Neuron.NodeData> Nodes1D
    {
        get { return Simulation.Neuron.nodes; }
    }



    //Could possibly pass count as parameter instead and just increment count locally from update()
    void preSynapticPlacement(RaycastHit hit)
    {
        if(count == 0)
        {
            int preSynapticIndex = Simulation.GetNearestPoint(hit);

            for (int i = 0; i < synapses.Count; i++)
            {
                if(synapses[i].nodeIndex == preSynapticIndex)
                {
                    return;
                }
            }

            Synapse pre = gameObject.AddComponent<Synapse>();

            pre.prefab = Instantiate(PrefabPreSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            pre.nodeIndex = preSynapticIndex;

            synapses.Add(pre);

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


    void postSynapticPlacement(RaycastHit hit)
    {
        if(count == 1)
        {
            Synapse post = gameObject.AddComponent<Synapse>();

            post.prefab = Instantiate(PrefabPostSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            int postSynapticIndex = Simulation.GetNearestPoint(hit);
            post.nodeIndex = postSynapticIndex;

            synapses.Add(post);

            post.transformRayCast(hit);
            synapseLocations.Add(post.prefab.transform.localPosition);

            count++;
        } 
    }


    void deleteSynapseHit(RaycastHit hit)
    {
        holdCount++;
        if(holdCount >= 50)
        {
            int hitIndex = Simulation.GetNearestPoint(hit);
            for(int i = 0; i < synapses.Count; i++)
            {
                //Add if synapses.count % 2 != 0 mean its not even dont delete
                if(synapses[i].nodeIndex == hitIndex && i % 2 == 0)
                {
                    //try catch(argumentoutofrangeexception)
                    Destroy(synapses[i]);
                    Destroy(synapses[i].prefab);
                    Destroy(synapses[i + 1]);
                    Destroy(synapses[i + 1].prefab);
                    synapses.RemoveAt(i);
                    synapses.RemoveAt(i);
                    //i -= 2;
                    holdCount = 0;
                    return;
                }
                else if(synapses[i].nodeIndex == hitIndex && i % 2 != 0)
                {
                    Destroy(synapses[i]);
                    Destroy(synapses[i].prefab);
                    Destroy(synapses[i - 1]);
                    Destroy(synapses[i - 1].prefab);
                    synapses.RemoveAt(i);
                    synapses.RemoveAt(i - 1);
                    //i -= 2;
                    holdCount = 0;
                    return;
                }
            }
            holdCount = 0;
        }
    }


    private void OnEnable()
    {
        // Trigger events used for raycasting to the neuron
        hitEvent = gameObject.AddComponent<RaycastPressEvents>();
        Simulation.raycastEventManager.LRTrigger = this.hitEvent;
        hitEvent.OnPress.AddListener((hit) => preSynapticPlacement(hit));
        hitEvent.OnHoldPress.AddListener((hit) => deleteSynapseHit(hit));
    }

    private void OnDisable()
    {
        SynapseManager.synapsesList = synapses;
        Destroy(GetComponent<RaycastPressEvents>());
    }

    void Update()
    {
        if (GameManager.instance.activeSim != null)
        {
            if (count == 2)
            {  
                for(int i = synapses.Count - 2; i < synapses.Count - 1; i++)
                {
                    if(i % 2 == 0)
                    {
                        GameObject arrowHead;

                        // Create a new arrow in 3D space
                        arrowHead = Instantiate(arrow, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                        
                        // Use Vector3 lerp so the position does not set it to the middle of the pre synapse but rather between the two
                        arrowHead.transform.position = Vector3.Lerp(synapses[i].prefab.transform.position, synapses[i + 1].prefab.transform.position, 0.5f);
                        arrowHead.transform.LookAt(synapses[i + 1].prefab.transform.position);

                        // Adjust the z scale of the arrow so we can point correctly to the post-synapse
                        // Also the -0.08f on the distance z variable is just the offset of the size of the synapse
                        arrowHead.transform.localScale = new Vector3(0.02f, 0.01f, Vector3.Distance(synapses[i].prefab.transform.position, synapses[i + 1].prefab.transform.position) - 0.08f);
                        arrowHead.transform.SetParent(synapses[i].prefab.transform);
                    }
                }
                count = 0;
            }
        }
    }
}
