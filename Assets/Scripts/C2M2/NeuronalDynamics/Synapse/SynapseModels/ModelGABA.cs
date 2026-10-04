using UnityEngine;

public class ModelGABA : ISynapseModel
{
    private string modelName;
    public ModelGABA() {
        modelName = "GABA";
    }

    /// This is the GABA Synapse function borrowed from Rothman, Jason S. "Modeling Synapses." (2014).
    /// </summary>
    /// <param name="v"></param> this is the postsynaptic voltage
    /// <param name="t"></param> this is the current simulation time
    /// <param name="ts"></param> this is the activation time of the synapse, this is NOT the time the synapse is placed
    /// <returns></returns>
    public double getModelCurrent(double v, double t, double ts)
    {
        // Erev = -80 mV (Destexhe, Mainen & Sejnowski 1998, Sec. 1.4.3's fit to whole-cell
        // GABA_A currents), below the -70 mV rest so the synapse hyperpolarizes. tau_d = 6.5 ms
        // (Kraushaar & Jonas 2000, as given by Roth & van Rossum 2009, Sec. 6.1). g is positive -
        // the outward-current sign is now handled once, correctly, at the call site (see
        // SynapseExplicitSBDF), not compensated here with a negative conductance (see
        // NeuroVISOR-CSharpStudies/src/csharp/GabaSynapse.cs).
        double Erev = -0.080;           // (Volts) GABA_A reversal potential
        double taud = 6.5e-3;           // (Seconds) decay constant
        double g = 1e-9;                // (Siemens) peak conductance

        return g * System.Math.Exp(-(t - ts) / taud) * (v - Erev);      
    }

    //Returns the model name
    public string getModelName()
    {
        return modelName;
    }

    //Boolean for synapse behavior, used for material of synapse
    public bool isExcitatory() {
        return false;
    }
}