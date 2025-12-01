using UnityEngine;
using System;

public class ModelGABA : ISynapseModel
{
    private string modelName;
    private double Erev;
    private double taud;
    private double g;
    private double Imax;
    private double voltageThreshold;   //Volts
    private double refireRate; // arbitrarily chosen
    private double minRefireTime; // ms
    public ModelGABA() {
        modelName = "GABA";
        Erev = -0.065;           // (Volts) Reversal potential of GABA synapses, stated on page 9 of Rothman's paper
        taud = 15.0e-3;           // (Seconds) decay constant from function, found in figure 2 of Rothman's Paper
        g = -10.0e-9;               // (Siemens) a chosen arbitrary value that produces a noticeable, but not too great, inhibitory response.
                                        // Rothman's paper does not provide any examples for max capacitance of GABA synapses

        double Vmax = 0.1;  // (Volts)
        Imax = System.Math.Abs(g * (Vmax - Erev));

        voltageThreshold = -0.05;   //Volts
        refireRate = 3.0; // arbitrarily chosen
        minRefireTime = 1.0e-2; // ms
    }

    /// This is the GABA Synapse function borrowed from Rothman, Jason S. "Modeling Synapses." (2014).
    /// </summary>
    /// <param name="v"></param> this is the postsynaptic voltage
    /// <param name="t"></param> this is the current simulation time
    /// <param name="ts"></param> this is the activation time of the synapse, this is NOT the time the synapse is placed
    /// <returns></returns>
    public double getModelCurrent(double v, double t, double ts)
    {
        return g * System.Math.Exp(-(t - ts) / taud) * (v - Erev);      
    }

    //Returns the model name
    public string getModelName()
    {
        return modelName;
    }

    public double getImax()
    {
        return Imax;
    }
    public double getTaud()
    {
        return taud;
    }
    
    //Boolean for synapse behavior, used for material of synapse
    public bool isExcitatory()
    {
        return false;
    }
    
    public bool isActive(double presynVoltage, double presynVoltagePrev, double ActivationTime, double simulationTime)
    {
        bool updateActivation = false;

        if ((presynVoltage >= voltageThreshold) && ((presynVoltagePrev < voltageThreshold) || (simulationTime - ActivationTime > refireRate*taud)) && (simulationTime - ActivationTime > minRefireTime))
        {
            Debug.Log("Activation Time Updated");
            updateActivation = true;
        }

        return updateActivation;

    }
}