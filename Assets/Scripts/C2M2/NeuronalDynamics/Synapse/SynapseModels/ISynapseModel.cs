using UnityEngine;

public interface ISynapseModel
{
    string getModelName();
    double getModelCurrent(double voltage, double t, double ts);
    bool isExcitatory();
}