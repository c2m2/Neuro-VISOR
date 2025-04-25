using System;
using UnityEngine;
public class CurrentClamp : ISynapseModel
{
    private string modelName;
    public CurrentClamp() {
        //Provides the Name and Material for the model
        modelName = "CLAMP";
        // modelMaterial = Resources.Load("ExcitatoryMat", typeof(Material)) as Material;
    }

    //Returns the Synaptic Current. Used in SparseSolver.  
    public double getModelCurrent(double v, double t, double ts)
    {
        return 39.0 * 1E-12;
    }

    public string getModelName()
    {
        return modelName;
    }

    public bool isExcitatory() {
        return true;
    }
}