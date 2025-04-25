using UnityEngine;
public class ModelAMPA : ISynapseModel
{
    private string modelName;
    public ModelAMPA() {
        //Provides the Name and Material for the model
        modelName = "AMPA";
        // modelMaterial = Resources.Load("ExcitatoryMat", typeof(Material)) as Material;
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

        from figure 2;
        n=2, Tr = 0.2ms, a1=0.9, taud1=0.3ms, a2=0.1, taud2=2.0ms

        Eampar is "typically 0 mv"

        To find Anorm: can be computed numerically by computing the product of the expressions
        in square brackets at high temporalresolution and setting anorm equal to the peak of the resulting waveform

        */

        double Erev = 0;        //From Rothman's paper: EampaR is "usually 0"       (V)
        double g = 1e-9;        //Calculated by using the graph in Rothman's Paper 
        double a1 = 0.9;        //Weight of first decay term of at
        double a2 = 0.1;        //Weight of second decay term of at
        double anorm = 1.0;     //Used to normalize the decay terms of at such that their summed maximum is always one.
        // Since a1 + a2 = 1, anorm is technically not necessary in this case, and has been set to 1
        double taud1 = 0.0003;  //First decay constant of at    (S)
        double taud2 = 0.002;   //Second decay constant of at   (S)
        double taur = 0.0002;   //Decay weight constant(?)      (S)
        int n = 2;              //Decay weight power(?)


        double at = System.Math.Pow(1 - System.Math.Exp(-(t-ts)/taur), n) * (a1*System.Math.Exp(-(t-ts)/taud1) + a2*System.Math.Exp(-(t-ts)/taud2))/anorm;
        
        //return g*at*(v-Erev);

        return (((0.9 * System.Math.Exp(-1.0*(t-ts)/taud1)) * (0.1 * System.Math.Exp(-1.0*(t-ts)/taud2)))/0.09) * g * (v - Erev);

    }

    public string getModelName()
    {
        return modelName;
    }

    public bool isExcitatory() {
        return true;
    }
}