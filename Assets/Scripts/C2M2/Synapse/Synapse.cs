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

    public SynapseManager synapseManager
    {
        get
        {
            return GameManager.instance.synapseManagerPrefab.GetComponent<SynapseManager>();
        }
    }

    public KeyCode modeChangeKey = KeyCode.Z;
    public bool ModeChange
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                return OVRInput.Get(OVRInput.Button.Two);
            }
            else
            {
                return Input.GetKey(modeChangeKey);
            }
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
        HitEvent.OnHover.AddListener((hit) => ChangeModel());
        HitEvent.OnHoldPress.AddListener((hit) => MonitorInput());
        HitEvent.OnEndPress.AddListener((hit) => CheckInput());
    }

    public void ChangeModel()
    {
        if (ModeChange)
        {
            if (currentModel == Model.GABA) SwitchModel(Model.NMDA);
            else SwitchModel(Model.GABA);
        }
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

    public void SwitchModel(Model model)
    {
        currentModel = model;

        Material mat;
        if (model == Model.NMDA)
        {
            mat = excitatoryMat;
        } else
        {
            mat = inhibitoryMat;
        }
        SwitchMaterial(mat);
    }
}
