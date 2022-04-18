using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Synapse : NDInteractables
{
    public Model currentModel = Model.NMDA;

    public enum Model
    {
        NMDA,
        GABA
    }

    public Neuron.NodeData NodeData
    {
        get
        {
            return simulation.Neuron.nodes[FocusVert];
        }
    }

    public GameObject prefab;
    public int nodeIndex;
    public NDSimulation attachedSim;

    /// <summary>
    /// Contructor so we can initilize values for each synapse
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="nodeIndex"></param>
    /// <param name="voltage"></param>
    public Synapse(GameObject prefab, int nodeIndex, NDSimulation attachedSim)
    {
        this.prefab = prefab;
        this.nodeIndex = nodeIndex;
        this.attachedSim = attachedSim;
    }

    override
    public string ToString()
    {
        return prefab.name + " " + nodeIndex;
    }

    /// <summary>
    /// Place the synapse prefab(sphere) onto the current neuron
    /// </summary>
    /// <param name="hit"></param>
    public void transformRayCast(RaycastHit hit)
    {
        simulation = hit.collider.GetComponentInParent<NDSimulation>();
        if (simulation.Neuron.somaIDs.Contains(nodeIndex))
        {
            //Transform the position of the synapse to where we raycast onto
            prefab.transform.SetParent(simulation.transform);
            prefab.transform.position = hit.point;
            FocusVert = nodeIndex;

            prefab.transform.localScale = new Vector3((float)NodeData.NodeRadius, (float)NodeData.NodeRadius, (float)NodeData.NodeRadius);
        }
        else
        {
            // Set the neuron as the parent of the synapse
            prefab.transform.SetParent(simulation.transform);
            FocusVert = nodeIndex;

            // Make sure to transform locally since we made the neuron the parent of the synapse
            prefab.transform.localPosition = FocusPos;

            float currentVisualizationScale = (float)simulation.VisualInflation;

            float radiusScalingValue = 3f * (float)NodeData.NodeRadius;
            float heightScalingValue = 1f * simulation.AverageDendriteRadius;

            //Ensures synapse is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            prefab.transform.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
        }
    }

    public override void Place(int index)
    {
        throw new NotImplementedException();
    }
}
