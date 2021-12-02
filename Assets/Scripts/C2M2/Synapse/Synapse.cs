using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Synapse : MonoBehaviour
{
    

    ////Refrence to gather the current Simulation
    public NDSimulation Simulation
    {
        get
        {
            return (NDSimulation)GameManager.instance.activeSims[0];
        }
    }

    public Neuron.NodeData NodeData
    {
        get
        {
            return Simulation.Neuron.nodes[focusVert];
        }
    }

    public int focusVert { get; private set; } = -1;

    /// <summary>
    /// returns the 3D vertex that we have clicked on
    /// </summary>
    public Vector3 FocusPos
    {
        get { return Simulation.Verts1D[focusVert]; }
    }


    public GameObject prefab;
    public int nodeIndex;
    public double voltage;

    /// <summary>
    /// Contructor so we can initilize values for each synapse
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="nodeIndex"></param>
    /// <param name="voltage"></param>
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

    /// <summary>
    /// Place the synapse prefab(sphere) onto the current neuron
    /// </summary>
    /// <param name="hit"></param>
    public void transformRayCast(RaycastHit hit)
    {
        if (Simulation.Neuron.somaIDs.Contains(nodeIndex))
        {
            //Transform the position of the synapse to where we raycast onto
            this.prefab.transform.SetParent(Simulation.transform);
            this.prefab.transform.position = hit.point;
            focusVert = nodeIndex;

            this.prefab.transform.localScale = new Vector3((float)NodeData.NodeRadius, (float)NodeData.NodeRadius, (float)NodeData.NodeRadius);
        }
        else
        {
            // Set the neuron as the parent of the synapse
            this.prefab.transform.SetParent(Simulation.transform);
            focusVert = nodeIndex;

            // Make sure to transform locally since we made the neuron the parent of the synapse
            this.prefab.transform.localPosition = FocusPos;

            //float scaleRatio = this.prefab.transform.localScale.x / 2;
            //float scaleRatio = Simulation.AverageDendriteRadius + (Simulation.AverageDendriteRadius / 2);

            //this.prefab.transform.localScale = new Vector3((float)NodeData.NodeRadius + scaleRatio, (float)NodeData.NodeRadius + scaleRatio, (float)NodeData.NodeRadius + scaleRatio);

            float currentVisualizationScale = (float)Simulation.VisualInflation;

            float radiusScalingValue = 3f * (float)NodeData.NodeRadius;
            float heightScalingValue = 1f * Simulation.AverageDendriteRadius;

            //Ensures clamp is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            //if (somaClamp) transform.parent.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
            this.prefab.transform.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
        }
    }
}
