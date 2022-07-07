using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using System;
using UnityEngine;

public class Synapse : NDInteractables
{
    public Model currentModel = Model.NMDA;

    public Material inhibitoryMat;
    public Material excitatoryMat;

    public double ActivationTime { get; set; }

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

    public SynapseManager SynapseManager
    {
        get
        {
            return GameManager.instance.simulationManager.synapseManager;
        }
    }

    private void OnDestroy()
    {
        SynapseManager.DeleteSyn(this);
    }

    public override void Place(int index)
    {
        SynapseManager.SynapticPlacement(this);

        transform.localPosition = FocusPos;
        float currentVisualizationScale = (float)simulation.VisualInflation;

        float radiusScalingValue = 3f * (float)NodeData.NodeRadius;
        float heightScalingValue = 1f * simulation.AverageDendriteRadius;

        //Ensures synapse is always at least as wide as tall when Visual Inflation is 1
        float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

        transform.localScale = new Vector3(radiusLength, radiusLength, radiusLength);

        SetToModeMaterial();
    }

    protected override void AddHitEventListeners()
    {
        HitEvent.OnHoldPress.AddListener((hit) => MonitorInput());
        HitEvent.OnEndPress.AddListener((hit) => CheckInput());
    }

    public void MonitorInput()
    {
        SynapseManager.HoldCount += Time.deltaTime;
        // If we've held the button long enough to destroy, color caps red until user releases button
        if (SynapseManager.HoldCount > SynapseManager.DestroyCount) SwitchMaterial(destroyMaterial);
    }

    private void CheckInput()
    {
        if (SynapseManager.HoldCount >= SynapseManager.DestroyCount)
        {
            SynapseManager.DeleteSyn(this);
        }
        else
        {
            if (currentModel == Model.GABA) SynapseManager.ChangeModel(this, Model.NMDA);
            else SynapseManager.ChangeModel(this, Model.GABA);
        }

        SynapseManager.HoldCount = 0;

        SetToModeMaterial();
    }

    public void SwitchModel(Model model)
    {
        currentModel = model;

        SetToModeMaterial();
    }

    public void SetToModeMaterial()
    {
        if (currentModel == Model.NMDA) SwitchMaterial(excitatoryMat);
        else SwitchMaterial(inhibitoryMat);
    }
}
