using UnityEngine;

public class ModelNMDA : ISynapseModel
{
    private string modelName;
    public ModelNMDA() {
        modelName = "NMDA";
    }

    //Returns the Synaptic Current. Used in SparseSolver.
    /// <summary>
    /// This is the NMDA Synapse function borrowed from Rothman, Jason S. "Modeling Synapses." (2014).
    /// </summary>
    /// <param name="v"></param> this is the postsynaptic voltage
    /// <param name="t"></param> this is the current simulation time
    /// <param name="ts"></param> this is the activation time of the synapse, this is NOT the time the synapse is placed
    /// <returns></returns>
    public double getModelCurrent(double v, double t, double ts)
    {
        /*
        Base equation:
        Inmdar = Gnmdar * a(t) * b(Vm) * (Vm - Enmdar)]

        Using equation 4 for a(t) and equation 10 for b(Vm)

        The following values were pulled directly from figure 3:
        v05=-0.0128, k=0.0224

        The following values were pulled directly from figure 2:
        taud=3.0e-4

        EnmdaR is "usually 0 mv" (page 7)

        Although the value for g used by the Rothman paper is 1e-9, an arbitrary value has been chosen that demonstrates synaptic behavior well
        */

        double Erev = 0;                // (Volts) reversal potential for synapse, Stated explicitly in Rothman's paper to usually be 0 Volts (page 7)
        // tau_d = 50 ms (Rothman & Silver 2014, Sec. 2's 30-70 ms NMDAR decay range), not the
        // 0.3 ms previously here - that is an AMPAR-range decay, not NMDA's. g = 1 nS, Rothman's
        // own published conductance (see NeuroVISOR-CSharpStudies/src/csharp/NmdaSynapse.cs).
        double taud = 50.0e-3;          // (Seconds) decay constant
        double g = 1e-9;                // (Siemens) peak conductance

        double v05 = -0.0128;           // (Volts) V0.5, first voltage constant used in Boltzmann function for Magnesium block, value found in figure 3 of Rothman paper
        double k = 0.0224;              // (Volts) second voltage constant used in Boltzmann function, value found in figure 3 of rothman paper

        return g * (1.0 / (1.0 + System.Math.Exp(-(v-v05) / k))) * System.Math.Exp(-(t - ts) / taud) * (v - Erev);          
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