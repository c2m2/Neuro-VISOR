using UnityEngine;

//Interface class used to define synapse models
public interface ISynapseModel
{
    string getModelName();
    double getModelCurrent(double voltage, double t, double ts);
    bool isExcitatory();
}