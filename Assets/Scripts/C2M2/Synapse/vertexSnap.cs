using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2;
using C2M2.NeuronalDynamics.UGX;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;

public class vertexSnap : MonoBehaviour
{
    public List<Vector3> SynapticNodeLocation = new List<Vector3>();
    bool temp = true;
    public GameObject PrefabPreSynapse;
    public GameObject PrefabPostSynapse;
    private int count = 0;
    public List<Synapse> synapses = new List<Synapse>();
    public Material material;
    public List<Vector3> synapseLocations = new List<Vector3>();
    public RaycastPressEvents hitEvent { get; private set; } = null;


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

    //Could possibly pass count as parameter instead and just inscrement count locally from update()
    void preSynapticPlacement(RaycastHit hit)
    {
        if(count == 0)
        {
            Synapse pre = gameObject.AddComponent<Synapse>();

            pre.prefab = Instantiate(PrefabPreSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            int preSynapticIndex = Simulation.GetNearestPoint(hit);
            pre.nodeIndex = preSynapticIndex;

            synapses.Add(pre);


            pre.transformPoint();
            synapseLocations.Add(pre.prefab.transform.localPosition);

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
            Synapse post = PrefabPostSynapse.AddComponent<Synapse>();

            post.prefab = Instantiate(PrefabPostSynapse, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

            int postSynapticIndex = Simulation.GetNearestPoint(hit);
            post.nodeIndex = postSynapticIndex;

            synapses.Add(post);

            post.transformPoint();
            synapseLocations.Add(post.prefab.transform.localPosition);

            count++;
        } 
    }

    private void Awake()
    {
        //Trigger events used for raycasting to the neuron
        hitEvent = gameObject.AddComponent<RaycastPressEvents>();
        Simulation.raycastEventManager.LRTrigger = this.hitEvent;
        hitEvent.OnPress.AddListener((hit) => preSynapticPlacement(hit));
    }

    void Update()
    {
        if (GameManager.instance.activeSim != null)
        {

            if (Input.GetKeyDown(KeyCode.G))
            {
                for(int i = 0; i < synapses.Count; i++)
                {
                    Debug.Log(synapses[i].ToString());
                }
            }
            

            if (count == 2)
            {
               

                for(int i = 0; i < synapses.Count - 1; i++)
                {
                    //TODO NEED TO OPTIMIZE THIS
                    if(synapses[i].prefab.GetComponent<LineRenderer>() == null)
                    {
                        LineRenderer line;
                        
                        line = synapses[i].prefab.AddComponent<LineRenderer>();
                        synapses[i + 1].prefab.AddComponent<LineRenderer>().SetVertexCount(0);
                        line.SetWidth(0.03F, 0.03F);
                        line.SetVertexCount(2);
                        line.material = material;

                        line.SetPosition(0, synapses[i].prefab.transform.position);
                        line.SetPosition(1, synapses[i + 1].prefab.transform.position);
                    }

                }

                count = 0;
            }

        }

    }
}
