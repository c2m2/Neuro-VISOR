using UnityEngine;
public class ModelAMPA : ISynapseModel
{
    private string modelName;
    public ModelAMPA() {
        //Provides the Name and Material for the model
        modelName = "AMPA";
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

        double Erev = 0;        //(Volts) From Rothman's paper: EampaR is "usually 0" (page 6)
        double g = 25e-9;       //(Siemens) An arbitrary value was chosen that clearly demonstrates AMPA's fast decay behavior, while
                                // still producing a noticeable (but not too large) post synaptic response
        double a1 = 0.9;        //(Unitless) Weight of first decay term of at
        double a2 = 0.1;        //(Unitless) Weight of second decay term of at
        double anorm = 1;       //(Unitless) Used to normalize the decay terms of at such that their summed maximum is always one.
                                // Since a1 + a2 = 1, anorm is technically not necessary in this case, and has been set to 1
        double taud1 = 0.0003;  //(Seconds) First decay constant of a(t)
        double taud2 = 0.002;   //(Seconds) Second decay constant of a(t)
        double taur = 0.0002;   //(Seconds) Decay weight constant
        int n = 2;              //(Unitless) Decay weight power


        double at = System.Math.Pow(1 - System.Math.Exp(-(t-ts)/taur), n) * (a1*System.Math.Exp(-(t-ts)/taud1) + a2*System.Math.Exp(-(t-ts)/taud2))/anorm;

        return g*at*(v-Erev);

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