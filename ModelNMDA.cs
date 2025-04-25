using UnityEngine;

public class ModelNMDA : ISynapseModel
{
    private string modelName;
    public ModelNMDA() {
        //Provides the Name and Material for the model
        modelName = "NMDA";
    }

    //Returns the Synaptic Current. Used in SparseSolver.
    public double getModelCurrent(double v, double t, double ts)
    {
        double Erev = 0;              // reversal potential for synapse
        double taud = 3.0e-4;         // decay constant from function
        double g = 1e-9;              // borrowed from Rothman Paper they mention 10's of nanosiemens

        double v05 = -0.0128;
        double k = 0.0224;

        // Debug.Log("(t-ts) = " + (t-ts));

        return g * (1.0 / (1.0 + System.Math.Exp(-(v-v05) / k))) * System.Math.Exp(-(t - ts) / taud) * (v - Erev);          
    }
    public string getModelName()
    {
        return modelName;
    }

    public bool isExcitatory() {
        return true;
    }
}