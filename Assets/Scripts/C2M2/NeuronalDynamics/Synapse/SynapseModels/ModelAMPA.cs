using UnityEngine;
using System;
public class ModelAMPA : ISynapseModel
{
    private string modelName;
    private double Erev;        //(Volts) From Rothman's paper: EampaR is "usually 0" (page 6)
    private double g;       //(Siemens) An arbitrary value was chosen that clearly demonstrates AMPA's fast decay behavior, while
                            // still producing a noticeable (but not too large) post synaptic response
                            // 10 pS per receptor, ~40 receptors per synapse, choose # of synapses
    private double a1;        //(Unitless) Weight of first decay term of at
    private double a2;        //(Unitless) Weight of second decay term of at
    private double anorm;       //(Unitless) Used to normalize the decay terms of at such that their summed maximum is always one.
                            // Since a1 + a2 = 1, anorm is technically not necessary in this case, and has been set to 1
    private double taud1;  //(Seconds) First decay constant of a(t)
    private double taud2;   //(Seconds) Second decay constant of a(t)
    private double taur;   //(Seconds) Decay weight constant
    private int n;              //(Unitless) Decay weight power
    private double Imax;
    private double voltageThreshold;   //Volts
    private double refireRate; // arbitrarily chosen
    private double minRefireTime; // ms
    public ModelAMPA() {
        //Provides the Name and Material for the model
        modelName = "AMPA";
        Erev = 0;        //(Volts) From Rothman's paper: EampaR is "usually 0" (page 6)
        g = 25e-9;       //(Siemens) An arbitrary value was chosen that clearly demonstrates AMPA's fast decay behavior, while
                                // still producing a noticeable (but not too large) post synaptic response
                                // 10 pS per receptor, ~40 receptors per synapse, choose # of synapses
        a1 = 0.9;        //(Unitless) Weight of first decay term of at
        a2 = 0.1;        //(Unitless) Weight of second decay term of at
        anorm = 1;       //(Unitless) Used to normalize the decay terms of at such that their summed maximum is always one.
                                // Since a1 + a2 = 1, anorm is technically not necessary in this case, and has been set to 1
        taud1 = 0.0003;  //(Seconds) First decay constant of a(t)
        taud2 = 0.002;   //(Seconds) Second decay constant of a(t)
        taur = 0.0002;   //(Seconds) Decay weight constant
        n = 2;              //(Unitless) Decay weight power

        double Vmax = 0.1;  // (Volts)
        Imax = System.Math.Abs(g * (Vmax - Erev));

        voltageThreshold = 0.038;   //Volts
        refireRate = 3.0; // arbitrarily chosen
        minRefireTime = 1.0e-2; // ms
    }

    //Returns the Synaptic Current. Used in SparseSolver.
    /// <summary>
    /// This is the AMPA Synapse function borrowed from Rothman, Jason S. "Modeling Synapses." (2014).
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

        Using equation 6 for a(t), ignoring 'extrasynaptic receptors'
        [1-exp(-(t-ts)/Tr)]^n * [a1* exp(-(t-ts)/taud) + (a2*exp(-(t-ts)/taud2))]/(anorm)

        The following values were pulled directly from figure 2;
        n=2, taur=0.2ms, a1=0.9, taud1=0.3ms, a2=0.1, taud2=2.0ms

        Eampar is "typically 0 mv"

        Although the value for g used by the Rothman paper is 1e-9, an arbitrary value has been chosen that demonstrates synaptic behavior well
        */

        double at = System.Math.Pow(1 - System.Math.Exp(-(t-ts)/taur), n) * (a1*System.Math.Exp(-(t-ts)/taud1) + a2*System.Math.Exp(-(t-ts)/taud2))/anorm;

        return g*at*(v-Erev);

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
    
    public bool isActive(double presynVoltage, double presynVoltagePrev, double ActivationTime)
    {
        double voltageThreshold = 0.038;   //Volts
        double refireRate = 3.0; // arbitrarily chosen
        double minRefireTime = 1.0e-2; // ms
        bool updateActivation = false;

        if ((presynVoltage >= voltageThreshold) && ((presynVoltagePrev < voltageThreshold) || (GetSimulationTime() - ActivationTime > refireRate*taud)) && (GetSimulationTime() - ActivationTime > minRefireTime))
        {
            Debug.Log("Activation Time Updated");
            updateActivation = true;
        }

        return updateActivation;

    }
}