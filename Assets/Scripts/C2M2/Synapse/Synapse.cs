using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Synapse : MonoBehaviour
{

    public NDSimulation curSimulation = null;

    public Neuron.NodeData NodeData
    {
        get
        {
            return curSimulation.Neuron.nodes[focusVert];
        }
    }

    public int focusVert { get; private set; } = -1;

    /// <summary>
    /// returns the 3D vertex that we have clicked on
    /// </summary>
    public Vector3 FocusPos
    {
        get { return curSimulation.Verts1D[focusVert]; }
    }


    public GameObject prefab;
    public int nodeIndex;
    public double voltage;
    public NDSimulation attachedSim;

    /// <summary>
    /// Contructor so we can initilize values for each synapse
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="nodeIndex"></param>
    /// <param name="voltage"></param>
    public Synapse(GameObject prefab, int nodeIndex, double voltage, NDSimulation attachedSim)
    {
        this.prefab = prefab;
        this.nodeIndex = nodeIndex;
        this.voltage = voltage;
        this.attachedSim = attachedSim;
    }

    override
    public string ToString()
    {
        return this.prefab.name + " " + this.nodeIndex + " " + voltage;
    }

    /// <summary>
    /// Place the synapse prefab(sphere) onto the current neuron
    /// </summary>
    /// <param name="hit"></param>
    public void transformRayCast(RaycastHit hit)
    {
        curSimulation = hit.collider.GetComponentInParent<NDSimulation>();
        if (curSimulation.Neuron.somaIDs.Contains(nodeIndex))
        {
            //Transform the position of the synapse to where we raycast onto
            this.prefab.transform.SetParent(curSimulation.transform);
            this.prefab.transform.position = hit.point;
            focusVert = nodeIndex;

            this.prefab.transform.localScale = new Vector3((float)NodeData.NodeRadius, (float)NodeData.NodeRadius, (float)NodeData.NodeRadius);
        }
        else
        {
            // Set the neuron as the parent of the synapse
            this.prefab.transform.SetParent(curSimulation.transform);
            focusVert = nodeIndex;

            // Make sure to transform locally since we made the neuron the parent of the synapse
            this.prefab.transform.localPosition = FocusPos;

            //float scaleRatio = this.prefab.transform.localScale.x / 2;
            //float scaleRatio = Simulation.AverageDendriteRadius + (Simulation.AverageDendriteRadius / 2);

            //this.prefab.transform.localScale = new Vector3((float)NodeData.NodeRadius + scaleRatio, (float)NodeData.NodeRadius + scaleRatio, (float)NodeData.NodeRadius + scaleRatio);

            float currentVisualizationScale = (float)curSimulation.VisualInflation;

            float radiusScalingValue = 3f * (float)NodeData.NodeRadius;
            float heightScalingValue = 1f * curSimulation.AverageDendriteRadius;

            //Ensures synapse is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            this.prefab.transform.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
        }
    }
}
