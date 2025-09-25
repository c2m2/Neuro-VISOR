using Boo.Lang;
using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
public class Synapse : NDInteractables
{
    //Defines a List to be used as the list of available synaptic models
    static ISynapseModel[] modelArray = {new ModelNMDA(), new ModelGABA(), new ModelAMPA()};
    static LinkedList<ISynapseModel> modelList = new LinkedList<ISynapseModel>(modelArray);
    public LinkedListNode<ISynapseModel> currentModel = modelList.First;

    //Defines the material for the model
    public Material inhibitoryMat;
    public Material excitatoryMat;
    public Material prePlaceMat;
    public int Id;


    public double ActivationTime { get; set; }

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
            //Implements the circularly linked list 
            if (currentModel.Value.Equals(modelList.Last.Value)) {
                SynapseManager.ChangeModel(SynapseManager.FindSelectedSyn(this), modelList.First.Value);
                currentModel = modelList.First;     //Resets to the first model in the linked list
            }
            else {
                SynapseManager.ChangeModel(SynapseManager.FindSelectedSyn(this), currentModel.Next.Value);
                currentModel = currentModel.Next;   //Iterates through the linked list
            }
            Debug.Log("Current Model: " + currentModel.Value);
        }
        // Delete synapse
        else if (SynapseManager.HoldCount >= SynapseManager.DestroyCount)
        {
            SynapseManager.DeleteSyn(SynapseManager.FindSelectedSyn(this));        }
        // Place synapse 
        else if (GameManager.instance.simulationManager.FeatState == NDSimulationManager.FeatureState.Synapse)
        {
            SynapseManager.SynapticPlacement(this);
        }
        SynapseManager.HoldCount = 0;
    }

    public void SwitchModel(ISynapseModel model)
    {
        currentModel = modelList.Find(model);
        SetToModeMaterial();
    }

    public void SetPrePlace()
    {
        SwitchMaterial(prePlaceMat);
    }

    public void SetToModeMaterial()
    {
        if (currentModel.Value.isExcitatory()) { SwitchMaterial(excitatoryMat); }
        else { SwitchMaterial(inhibitoryMat); }
    }
}