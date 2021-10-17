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
    List<Vector3> NodeLocation = new List<Vector3>();
    int count = 0;
    bool temp = true;
    GameObject PreSynapse;
    public GameObject PostSynapse;
    private LineRenderer line;

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

    //Accessing the node data of the neuron
    public List<Neuron.NodeData> Nodes1D
    {
        get { return Simulation.Neuron.nodes; }
    }

    public int focusVert { get; private set; } = -1;
    public Vector3 FocusPos
    {
        get { return Simulation.Verts1D[focusVert]; }
    }


    void Start()
    {
        SynapseLocation = this.gameObject.transform.position;
        PreSynapse = this.gameObject;

        // Add a Line Renderer to the GameObject
        line = this.gameObject.AddComponent<LineRenderer>();
        // Set the width of the Line Renderer
        line.SetWidth(0.03F, 0.03F);
        // Set the number of vertex fo the Line Renderer
        line.SetVertexCount(2);
    }

    //Getting the boundary nodes (Dendrites) into a Vector3 List
    List<Vector3> getBoundaryNodes()
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

    //Get the vertices of the current neuron
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

    //TODO MAKE A LIST OF THE SYNAPSES AND ALLOW USER TO PLACE TWO THAT DRAW A LINE AND CONNECT THEM

    void preSynapsePlace()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            try
            {
                if (hit.collider.transform.parent.gameObject == GameObject.Find("(Solver)SparseSolverTestv1"))
                {
                    PreSynapse.transform.position = hit.point;
                }
            }
            catch (System.NullReferenceException)
            {
                //TODO MAKE CATCH THEN RETURN THE POSITION OF THE GAMEOBJECT IS THE NEURON (do not look for parent)
                Debug.Log("Did not find Nueron");
            }

        }
        PreSynapse.transform.SetParent(Simulation.transform);
        for (int i = 0; i < NodeLocation.Count; i++)
        {
            Debug.Log(Vector3.Distance(PreSynapse.transform.localPosition, NodeLocation[i]));
            if (Vector3.Distance(PreSynapse.transform.localPosition, NodeLocation[i]) <= 5.0f)
            {
                Debug.Log(NodeLocation[i]);
                PreSynapse.transform.localPosition = NodeLocation[i];
                break;
            }
        }
        PreSynapse.transform.parent = null;
    }

    void postSynapsePlace()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            try
            {
                if (hit.collider.transform.parent.gameObject == GameObject.Find("(Solver)SparseSolverTestv1"))
                {
                    PostSynapse.transform.position = hit.point;
                }
            }
            catch (System.NullReferenceException)
            {
                //TODO MAKE CATCH THEN RETURN THE POSITION OF THE GAMEOBJECT IS THE NEURON (do not look for parent)
                Debug.Log("Did not find Nueron");
            }

        }
        PostSynapse.transform.SetParent(Simulation.transform);
        for (int i = 0; i < NodeLocation.Count; i++)
        {
            //Debug.Log(Vector3.Distance(PreSynapse.transform.localPosition, NodeLocation[i]));
            if (Vector3.Distance(PostSynapse.transform.localPosition, NodeLocation[i]) <= 5.0f)
            {
                Debug.Log(NodeLocation[i]);
                PostSynapse.transform.localPosition = NodeLocation[i];
                break;
            }
        }
        PostSynapse.transform.parent = null;
    }

    void Update()
    {
        if (GameManager.instance.activeSim != null)
        {
            //Find the Nodes just once per simulation or else this leads to very bad locations
            if (temp)
            {
                //dendriteLocation = getBoundaryNodes();
                NodeLocation = getNodeData();
                temp = false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                preSynapsePlace();
                line.SetPosition(0, PreSynapse.transform.position);
                line.SetPosition(1, PostSynapse.transform.position);
            }
            if (Input.GetMouseButtonDown(1))
            {
                postSynapsePlace();
                line.SetPosition(0, PreSynapse.transform.position);
                line.SetPosition(1, PostSynapse.transform.position);
            }

        }

        //If the simulation is ended put back to original location
        if (GameManager.instance.activeSim == null)
        {
            PreSynapse.transform.parent = null;
            transform.position = Vector3.MoveTowards(transform.position, SynapseLocation, (float).03);
            temp = true;
        }

    }
}
