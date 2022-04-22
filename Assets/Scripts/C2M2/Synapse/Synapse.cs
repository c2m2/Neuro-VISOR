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

        transform.SetParent(simulation.transform);
        transform.localPosition = FocusPos;
        if (simulation.Neuron.somaIDs.Contains(FocusVert))
        {
            transform.localScale = new Vector3((float)NodeData.NodeRadius, (float)NodeData.NodeRadius, (float)NodeData.NodeRadius);
        }
        else
        {
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
        HitEvent.OnHoldPress.AddListener((hit) => MonitorInput());
        HitEvent.OnEndPress.AddListener((hit) => CheckInput());
    }

    public void MonitorInput()
    {
        if (synapseManager.PressedCancel || !synapseManager.PressedInteract)
        {
            CheckInput();
        }
        else
        {
            synapseManager.HoldCount += Time.deltaTime;

            // If we've held the button long enough to destroy, color caps red until user releases button
            if (synapseManager.HoldCount > synapseManager.DestroyCount) SwitchMaterial(destroyMaterial);
        }
    }

    private void CheckInput()
    {
        if (!synapseManager.PressedCancel)
        {
            if (synapseManager.HoldCount >= synapseManager.DestroyCount)
            {
                synapseManager.DeleteSyn(this);
            }
        }

        synapseManager.HoldCount = 0;
        SwitchMaterial(defaultMaterial);
    }
}
