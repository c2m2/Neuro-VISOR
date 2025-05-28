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
        double Erev = -0.065;           // (Volts) Reversal potential of GABA synapses, stated on page 9 of Rothman's paper
        double taud = 3.0e-4;           // (Seconds) decay constant from function, found in figure 2 of Rothman's Paper
        double g = -1e-9;               // (Siemens) a chosen arbitrary value that produces a noticeable, but not too great, inhibitory response.
                                        // Rothman's paper does not provide any examples for max capacitance of GABA synapses

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