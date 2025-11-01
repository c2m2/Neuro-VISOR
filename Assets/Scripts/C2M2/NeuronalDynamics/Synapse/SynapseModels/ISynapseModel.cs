using UnityEngine;

//Interface class used to define synapse models
public interface ISynapseModel
{
    string getModelName();
    double getModelCurrent(double voltage, double t, double ts);
    bool isExcitatory();
    bool isActive(double presynVoltage, double presynVoltagePrev, double ActivationTime, double simulationTime);
    double getImax();
}