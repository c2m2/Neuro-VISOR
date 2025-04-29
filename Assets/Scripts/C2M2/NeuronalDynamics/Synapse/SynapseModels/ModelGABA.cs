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
        double Erev = -0.065;              // reversal potential for synapse
        double taud = 3.0e-4;               // decay constant from function
        double g = 30e-12;              // borrowed from Rothman Paper this is conductance of GABA receptor

        return -g * System.Math.Exp(-1.0 * (t - ts) / taud) * (v - Erev);      
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