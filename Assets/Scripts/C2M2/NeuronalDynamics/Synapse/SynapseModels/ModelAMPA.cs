using UnityEngine;
using System;

/*
Reference:

Rothman, J.S. (2014). Modeling Synapses. In: Jaeger, D., Jung, R. (eds) 
Encyclopedia of Computational Neuroscience. Springer, New York, NY. 
https://doi.org/10.1007/978-1-4614-7320-6_240-1
*/

public class ModelAMPA : ISynapseModel
{
    private string modelName;
    private double Erev;        //(Volts) From Rothman's paper: EampaR is "usually 0" (page 6)
    private double g;       //(Siemens) An arbitrary value was chosen that clearly demonstrates AMPA's fast decay behavior, while
                            // still producing a noticeable (but not too large) post synaptic response
                            // 10 pS per receptor, ~40 receptors per synapse, choose # of synapses
    private double anorm;       // (Unitless) Used to normalize the decay terms of at such that their summed maximum is always one.
    private double sigma;    // (Unitless) Used to scale the noise added to the synaptic current
    private double taud;  // (Seconds) First decay constant of a(t)
    private double beta;    // (Unitless) Proportion of taur to taud
    private double taur;   //(Seconds) Decay weight constant
    private int n;              //(Unitless) Decay weight exponent
    private double Imax;
    private double voltageThreshold;   //Volts
    private double refireRate; // refire at 5%
    private double minRefireTime; // ms
    public ModelAMPA() {
        //Provides the Name and Material for the model
        modelName = "AMPA";
        Erev = 0;        //(Volts) From Rothman's paper: EampaR is "usually 0" (page 6)
        g = 25e-9;       //(Siemens) An arbitrary value was chosen that clearly demonstrates AMPA's fast decay behavior, while
                                // still producing a noticeable (but not too large) post synaptic response
                                // 10 pS per receptor, ~40 receptors per synapse, choose # of synapses

        taud = 4.0e-3;  //(Seconds) Decay constant of a(t)
        beta = 0.05;    // (Unitless) Proportion of taur to taud
        taur = beta * taud;   //(Seconds) Decay weight constant
        n = 2;              //(Unitless) Decay weight power

        sigma = 1 / (n * (taud / taur) + 1); // (Unitless) Used to scale the noise added to the synaptic current
        anorm = System.Math.Pow(1 - sigma, n) * System.Math.Pow(sigma, taur/taud);       //(Unitless) Used to normalize the decay terms of at such that their summed maximum is always one.

        double Vmax = 0.1;  // (Volts)
        Imax = System.Math.Abs(g * (Vmax - Erev));

        voltageThreshold = 0.038;   //Volts
        refireRate = 3.0; // refire at 5%
        minRefireTime = 1.0e-2; // ms
    }

    //Returns the Synaptic Current. Used in SparseSolver.
    /// <summary>
    /// This is the AMPA Synapse function borrowed from Rothman.
    /// </summary>
    /// <param name="v"></param> this is the postsynaptic voltage
    /// <param name="t"></param> this is the current simulation time
    /// <param name="ts"></param> this is the activation time of the synapse, this is NOT the time the synapse is placed
    /// <returns></returns>    
    public double getModelCurrent(double v, double t, double ts)
    {
        /*
        Base Equation:
        Iampar = GampaR * a(t) * (V(m) - Eampar)

        The following values were pulled directly from figure 2;
        n=2

        Eampar is "typically 0 mv"

        Although the value for g used by the Rothman is 1e-9, an arbitrary value has been chosen that demonstrates synaptic behavior well
        */

        double at = System.Math.Pow(1 - System.Math.Exp(-(t-ts)/taur), n) * System.Math.Exp(-(t-ts)/taud) / anorm;
        double current = g*at*(v-Erev);

        return current;

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

    //Boolean for synapse behavior, used for material of synapse
    public bool isExcitatory()
    {
        return true;
    }
    
    public bool isActive(double presynVoltage, double presynVoltagePrev, double SimulationTime, double ActivationTime)
    {
        bool updateActivation = false;

        if ((presynVoltage >= voltageThreshold) && ((presynVoltagePrev < voltageThreshold) || (SimulationTime - ActivationTime > refireRate*taud)) && (SimulationTime - ActivationTime > minRefireTime))
        {
            Debug.Log("Activation Time Updated");
            updateActivation = true;
        }

        return updateActivation;

    }
}