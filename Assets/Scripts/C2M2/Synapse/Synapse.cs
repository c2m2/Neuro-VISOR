using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using System;
using UnityEngine;

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

    public SynapseManager synapseManager
    {
        get
        {
            return GameManager.instance.synapseManagerPrefab.GetComponent<SynapseManager>();
        }
    }

    public override void Place(int index)
    {
        synapseManager.SynapticPlacement(this);

        if (simulation.Neuron.somaIDs.Contains(FocusVert))
        {
            //Transform the position of the synapse to where we raycast onto
            transform.SetParent(simulation.transform);
            transform.localPosition = FocusPos;

            transform.localScale = new Vector3((float)NodeData.NodeRadius, (float)NodeData.NodeRadius, (float)NodeData.NodeRadius);
        }
        else
        {
            // Set the neuron as the parent of the synapse
            transform.SetParent(simulation.transform);
            FocusVert = FocusVert;

            // Make sure to transform locally since we made the neuron the parent of the synapse
            transform.localPosition = FocusPos;

            float currentVisualizationScale = (float)simulation.VisualInflation;

            float radiusScalingValue = 3f * (float)NodeData.NodeRadius;
            float heightScalingValue = 1f * simulation.AverageDendriteRadius;

            //Ensures synapse is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            transform.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
        }
    }

    protected override void AddHitEventListeners()
    {
        throw new NotImplementedException();
    }
}
