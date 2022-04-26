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

    public KeyCode modeChangeKey = KeyCode.Z;
    public bool ModeChange
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                return OVRInput.GetDown(OVRInput.Button.Two);
            }
            else
            {
                return Input.GetKeyDown(modeChangeKey);
            }
        }
    }

    public override void Place(int index)
    {
        SynapseManager.SynapticPlacement(this);

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

        SetToModeMaterial();
    }

    protected override void AddHitEventListeners()
    {
        HitEvent.OnHover.AddListener((hit) => ChangeModel());
        HitEvent.OnHoldPress.AddListener((hit) => MonitorInput());
        HitEvent.OnEndPress.AddListener((hit) => CheckInput());
    }

    public void ChangeModel()
    {
        if (ModeChange)
        {
            if (currentModel == Model.GABA) SynapseManager.ChangeModel(this, Model.NMDA);
            else SynapseManager.ChangeModel(this, Model.GABA);
        }
    }

    public void MonitorInput()
    {
        if (SynapseManager.PressedCancel || !SynapseManager.InteractHold)
        {
            CheckInput();
        }
        else
        {
            SynapseManager.HoldCount += Time.deltaTime;

            // If we've held the button long enough to destroy, color caps red until user releases button
            if (SynapseManager.HoldCount > SynapseManager.DestroyCount) SwitchMaterial(destroyMaterial);
        }
    }

    private void CheckInput()
    {
        if (!SynapseManager.PressedCancel)
        {
            if (SynapseManager.HoldCount >= SynapseManager.DestroyCount)
            {
                SynapseManager.DeleteSyn(this);
            }
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
