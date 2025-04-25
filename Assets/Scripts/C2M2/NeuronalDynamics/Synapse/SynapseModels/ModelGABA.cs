using UnityEngine;

public class ModelGABA : ISynapseModel
{
    private string modelName;
    public ModelGABA() {
        //Provides the Name and Material for the model
        modelName = "GABA";
        // modelMaterial = Resources.Load("ExcitatoryMat", typeof(Material)) as Material;
    }
    
    //Returns the Synaptic Current. Used in SparseSolver.
    public double getModelCurrent(double v, double t, double ts)
    {
        double Erev = -0.065;              // reversal potential for synapse
        double taud = 3.0e-4;               // decay constant from function
        double g = 30e-12;              // borrowed from Rothman Paper this is conductance of GABA receptor

        return -g * System.Math.Exp(-1.0 * (t - ts) / taud) * (v - Erev);      
    }

    public string getModelName()
    {
        return modelName;
    }

    public bool isExcitatory() {
        return false;
    }
}