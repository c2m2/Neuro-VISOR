using UnityEngine;

public class ModelNMDA : ISynapseModel
{
    private string modelName;
    public ModelNMDA() {
        modelName = "NMDA";
    }

    //Returns the Synaptic Current. Used in SparseSolver.
    /// <summary>
    /// This is the NMDA Synapse function borrowed from Rothman, Jason S. "Modeling Synapses." (2014).
    /// </summary>
    /// <param name="v"></param> this is the postsynaptic voltage
    /// <param name="t"></param> this is the current simulation time
    /// <param name="ts"></param> this is the activation time of the synapse, this is NOT the time the synapse is placed
    /// <returns></returns>
    public double getModelCurrent(double v, double t, double ts)
    {
        double Erev = 0;              // reversal potential for synapse (V)
        double taud = 3.0e-4;         // decay constant from function (sec)
        double g = 1e-9;              // borrowed from Rothman Paper they mention 10's of nanosiemens

        double v05 = -0.0128;         // V0.5, first voltage constant used in Boltzmann function for Magnesium block, (V)
        double k = 0.0224;            // second voltage constant used in Boltzmann function, (V)

        return g * (1.0 / (1.0 + System.Math.Exp(-(v-v05) / k))) * System.Math.Exp(-(t - ts) / taud) * (v - Erev);          
    }
    
    //Returns the model name
    public string getModelName()
    {
        return modelName;
    }

    //Boolean for synapse behavior, used for material of synapse
    public bool isExcitatory() {
        return true;
    }
}