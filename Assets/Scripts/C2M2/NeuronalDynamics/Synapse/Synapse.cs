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
    
    public int Id;

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
        SynapseManager.DeleteSyn(SynapseManager.FindSelectedSyn(this));
    }
    
    // Creates a unique synapse instance 
    public Synapse Clone()
    {
        System.Random rnd = new System.Random();
        Synapse other = (Synapse) this.MemberwiseClone();
        other.Id = rnd.Next();
        return other;
    }

    public override void Place(int index)
    {
        transform.localPosition = FocusPos;
        float currentVisualizationScale = (float)simulation.VisualInflation;
        float radiusScalingValue = 3f * (float)NodeData.NodeRadius;
        float heightScalingValue = 1f * simulation.AverageDendriteRadius;
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
        // Change model 
        if (SynapseManager.HoldCount >= SynapseManager.ChangeCount && SynapseManager.HoldCount <= SynapseManager.DestroyCount)
        {
            if (SynapseManager.FindSelectedSyn(this).currentModel == Model.GABA) SynapseManager.ChangeModel(SynapseManager.FindSelectedSyn(this), Model.NMDA);
            else SynapseManager.ChangeModel(SynapseManager.FindSelectedSyn(this), Model.GABA);
        }
        // Delete synapse
        else if (SynapseManager.HoldCount >= SynapseManager.DestroyCount)
        {
            SynapseManager.DeleteSyn(SynapseManager.FindSelectedSyn(this));
        }
        // Place synapse 
        else if (GameManager.instance.simulationManager.FeatState == NDSimulationManager.FeatureState.Synapse)
        {
            SynapseManager.SynapticPlacement(this); 
        }
        SynapseManager.HoldCount = 0;
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
