using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Synapse : MonoBehaviour
{
    

    ////Refrence to gather the current Simulation
    public NDSimulation Simulation
    {
        get
        {
            if (GameManager.instance.activeSim != null) return (NDSimulation)GameManager.instance.activeSim;
            else return GetComponentInParent<NDSimulation>();
        }
    }

    public int focusVert { get; private set; } = -1;


    public Vector3 FocusPos
    {
        get { return Simulation.Verts1D[focusVert]; }
    }

    public List<Neuron.NodeData> Nodes1D
    {
        get { return Simulation.Neuron.nodes; }
    }

    // get all the vertices of the current neuron
    List<Vector3> getNodeData()
    {
        List<Vector3> Nodes = new List<Vector3>();
        if (GameManager.instance.activeSim != null)
        {
            //For all vertices make a new Vector3 list of them so I can tranform it into 3d space
            for (int i = 0; i < Nodes1D.Count; i++)
            {
                Nodes.Add(new Vector3(((float)Nodes1D[i].Xcoords), ((float)Nodes1D[i].Ycoords), ((float)Nodes1D[i].Zcoords)));
            }
        }
        return Nodes;
    }


    //Contructor to intilizae some values
    public GameObject prefab;
    public int nodeIndex;
    public double voltage;
    public Synapse(GameObject prefab, int nodeIndex, double voltage)
    {
        this.prefab = prefab;
        this.nodeIndex = nodeIndex;
        this.voltage = voltage;
    }

    override
    public string ToString()
    {
        return this.prefab.name + " " + this.nodeIndex + " " + voltage;
    }


    public void transformRayCast(RaycastHit hit)
    {
        //TODO NEED FIXED FOR SOMA PLACEMENT / VOLTAGE
        if (Simulation.Neuron.somaIDs.Contains(nodeIndex))
        {
            this.prefab.transform.position = hit.point;
        }
        else
        {
            this.prefab.transform.SetParent(Simulation.transform);
            focusVert = nodeIndex;
            this.prefab.transform.localPosition = FocusPos;
        }
    }


    // This method will place the synapse on the point we clicked on the neuron(NOTE: works only on desktop version)
    public void transformPoint()
    {
        List<Vector3> nodes = getNodeData();
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            try
            {
                if (hit.collider.transform.parent.gameObject == GameObject.Find("(Solver)SparseSolverTestv1"))
                {
                    this.prefab.transform.position = hit.point;
                }
            }
            catch (System.NullReferenceException)
            {
                //TODO MAKE CATCH THEN RETURN THE POSITION OF THE GAMEOBJECT IS THE NEURON (do not look for parent)
                Debug.Log("Did not find Nueron");
                return;
            }

        }
        this.prefab.transform.SetParent(Simulation.transform);

        float smallest = Vector3.Distance(this.prefab.transform.localPosition, nodes[0]);

        Vector3 smallestPos = nodes[0];

        // For-loop to find the smallest distance of the vertice we placed the synapse next to
        for (int i = 0; i < nodes.Count; i++)
        {
            float current = Vector3.Distance(this.prefab.transform.localPosition, nodes[i]);
            
            if(smallest > current)
            {
                smallest = current;
                smallestPos = nodes[i];
            }
            
        }

        this.prefab.transform.localPosition = smallestPos;
    }

}
