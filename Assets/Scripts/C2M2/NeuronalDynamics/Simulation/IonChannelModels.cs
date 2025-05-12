using System;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra;

namespace C2M2.NeuronalDynamics.Simulation
{
    /// <summary>
    /// This file consists of the channels for each Ion Channel. The equations follow the class architecture listed in IonChannels.cs
    /// 
    /// </summary>
    public static class IonChannelModels
    {
        /// <summary>
        /// Potassium channel equations
        /// gK = 5.0e1, eK = -90e-3
        /// Equations derived from pospischil's paper
        /// </summary>
        public static IonChannel PotassiumChannel(int nodeCount)
        {
            /// <summary>
            /// [S/m2] potassium conductance per unit area, this is the Potassium conductance per unit area, it is used in this term
            /// ḡKd n^4 (V − ek)
            /// where n is the state variable, and ek is the reversal potential.
            /// </summary>
            double gk = 5.0 * 1.0E1;
            /// <summary>
            /// [V] potassium reversal potential
            /// </summary>
            double ek = -90.0 * 1.0E-3;
            /// <summary>
            /// [V] voltage threshold
            /// </summary>
            double vT = 0.0 * 1.0E-3;

            IonChannel potassiumChannel = new IonChannel("Potassium Channel", gk, ek);

            Func<Vector, Vector> alpha_n = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin); 
                return (1.0E3) * (0.032) * (15.0 + vT - Vin).PointwiseDivide(((15.0 + vT - Vin) / 5.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> beta_n = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.5) * ((10.0 + vT - Vin) / 40.0).PointwiseExp();
            };

            potassiumChannel.AddGatingVariable(
                new GatingVariable("n", alpha_n, beta_n, 4, 0.0376969, nodeCount)
            );

            return potassiumChannel;
        }

        /// <summary>
        /// Sodium channel equations
        /// </summary>
        public static IonChannel SodiumChannel(int nodeCount)
        {
            /// <summary>
            /// [S/m2] sodium conductance per unit area, this is the Sodium conductance per unit area, it is used in this term
            /// \f[\bar{g}_{Na}m^3h(V-V_{Na})\f]
            /// where \f$m,h\f$ are the state variables, and \f$V_{Na}\f$ is the reversal potential for sodium.
            /// </summary>
            double gna = 50.0 * 1.0E1;
            /// <summary>
            /// [V] sodium reversal potential
            /// </summary>
            double ena = 50.0 * 1.0E-3;
            /// <summary>
            /// [V] voltage threshold
            /// </summary>
            double vT = 0.0 * 1.0E-3;

            IonChannel sodiumChannel = new IonChannel("Sodium Channel", gna, ena);

            // alpha_m(V)
            Func<Vector, Vector> alpha_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.32) * (13.0 + vT - Vin).PointwiseDivide(((13.0 + vT - Vin) / 4.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> beta_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.28) * (Vin - vT - 40.0).PointwiseDivide(((Vin - vT - 40.0) / 5.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> alpha_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.128) * ((17.0 + vT - Vin) / 18.0).PointwiseExp();
            };

            Func<Vector, Vector> beta_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * 4.0 / (((40.0 + vT - Vin) / 5.0).PointwiseExp() + 1.0);
            };

            sodiumChannel.AddGatingVariable(
                new GatingVariable("m", alpha_m, beta_m, 3, 0.0147567, nodeCount)
            );
            sodiumChannel.AddGatingVariable(
                new GatingVariable("h", alpha_h, beta_h, 1, 0.9959410, nodeCount)
            );

            return sodiumChannel;
        }

        /// <summary>
        /// Leakage channel
        /// </summary>
        
        public static IonChannel LeakageChannel(int nodeCount)
        {
            /// <summary>
            /// [S/m2] leak conductance per unit area, this is the leak conductance per unit area, it is used in this term
            /// \f[\bar{g}_{l}(V-V_l)\f]
            /// </summary>
            double gl = 1.0E-4 * 1.0E4;  // S/m²
            /// <summary>
            /// [V] leak reversal potential
            /// </summary>
            double el = -70.0 * 1.0E-3;   // mV
            return new IonChannel("Leakage Channel", gl, el);
        }
            

        /// <summary>
        /// Calcium channel matching the solver's definitions verbatim
        /// gCa = 1.0e1, eCa = 120.0e-3
        /// alpha_q, beta_q, alpha_r, beta_r exactly as in SparseSolverTestv1
        /// </summary>
        public static IonChannel CalciumChannel(int nodeCount)
        {
            double gca = 0.01 * 1.0E3;
            double eca = 120.0 * 1.0E-3;

            IonChannel calciumChannel = new IonChannel("Calcium Channel", gca, eca);

            Func<Vector, Vector> alpha_q = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.055) * (27.0 - Vin) / (((-27.0 - Vin) / 3.8).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> beta_q = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.94) * (((-75.0 - Vin) / 17.0).PointwiseExp());
            };

            Func<Vector, Vector> alpha_r = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.000457) * (((-13.0 - Vin) / 50.0).PointwiseExp());
            };

            Func<Vector, Vector> beta_r = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.0065) / (((-15.0 - Vin) / 28.0).PointwiseExp() + 1.0);
            };

            calciumChannel.AddGatingVariable(
                new GatingVariable("q", alpha_q, beta_q, 2, 0.0, nodeCount)
            );
            calciumChannel.AddGatingVariable(
                new GatingVariable("r", alpha_r, beta_r, 1, 0.0, nodeCount)
            );

            return calciumChannel;
        }

        /// <summary>
        /// Slow Potassium Channel (I_M) from Yamada et al. (1989)
        ///   p∞(V) = 1 / [1 + exp(-(V+35)/10)]
        ///   τp(V) = τmax / [3.3 * exp((V+35)/20) + exp(-(V+35)/20)]
        ///   dp/dt = ( p∞(V) - p ) / τp(V)
        /// We use alpha_p(V) = p∞(V/)τp(V), beta_p(V) = [1 - p∞(V)]/τp(V).
        /// Default gM = 0.004 mS/cm² = 0.004 * 10 = 0.04 S/m², τmax = 4.0 s
        /// </summary>
        public static IonChannel SlowPotassiumChannel(int nodeCount)
        {
            // Convert 0.004 mS/cm² to S/m² by multiplying by 10.
            // 0.004 mS/cm² => 0.04 S/m²
            double gM   = 7.5E-5 * 1.0E4; // S/m^2
            double eK   = -90.0 * 1.0E-3; // -90 mV in [V]
            double tMax = .608;          // 4 s, per Yamada et al.

            // Create an IonChannel object with the specified max conductance and reversal potential
            IonChannel slowKChannel = new IonChannel("Slow Potassium Channel", gM, eK);

            // alpha_p(V) = p∞(V) / τp(V)
            Func<Vector, Vector> alpha_p = voltage =>
            {
                // Convert voltage from [V] to [mV]
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);

                // Compute steady-state value p∞(V)
                Vector pInf = 1.0 / (1.0 + (-(Vin + 35) / 10).PointwiseExp());

                // Compute time constant τp(V)
                Vector tauP = tMax / (3.3 * ((Vin + 35) / 20).PointwiseExp() + (-(Vin + 35) / 20).PointwiseExp());

                // Return alpha_p(V) = p∞(V) / τp(V)
                return pInf.PointwiseDivide(tauP);
            };

            // Define beta_p(V) = [1 - p∞(V)] / τp(V)
            Func<Vector, Vector> beta_p = voltage =>
            {
                // Convert voltage from [V] to [mV]
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);

                // Compute steady-state value p∞(V)
                Vector pInf = 1.0 / (1.0 + (-(Vin + 35) / 10).PointwiseExp());

                // Compute time constant τp(V)
                Vector tauP = tMax / (3.3 * ((Vin + 35) / 20).PointwiseExp() + (-(Vin + 35) / 20).PointwiseExp());

                // Return beta_p(V) = [1 - p∞(V)] / τp(V)
                return (1 - pInf).PointwiseDivide(tauP);
            };

            // Add the gating variable 'p' with exponent = 1 and initial probability 0.0.
            slowKChannel.AddGatingVariable(
                new GatingVariable("p", alpha_p, beta_p, 1, 0.0, nodeCount)
            );

            return slowKChannel;
        }

        /// <summary>
        /// Low Threshold Calcium Channel (T-current) from the snippet:
        /// 
        /// IT = gT * [ s∞(V) ]^2 * u * (V - Eca)
        /// 
        /// Where:
        ///   s∞(V) = 1 / [1 + exp(-(V + Vx + 57)/6.2)]
        ///   u∞(V) = 1 / [1 + exp((V + Vx + 81)/4)]
        ///   τu(V) = [ 30.8 + 211.4 + exp((V + Vx + 113.2)/5) ]
        ///            / [ 3.7 * (1 + exp((V + Vx + 84)/3.2)) ]
        /// 
        /// The activation s∞(V) is "instantaneous," so we do not create
        /// a separate gating variable for s. Instead, we define only
        /// the inactivation gating variable "u" with alpha_u/beta_u
        /// so that  du/dt = [u∞(V) - u]/τu(V).
        /// 
        /// Vx is a uniform shift of the voltage dependence, e.g. +2 mV.
        /// Default gT might be something like 0.05 S/m², or set as needed.
        /// 
        /// Note: The final current in your PDE solver would be:
        ///   I_T = gT * s∞(V)^2 * u * (V - Eca).
        /// </summary>
        public static IonChannel LowThresholdCalciumChannel(int nodeCount)
        {
            // Maximal conductance in S/m² (adjust as needed)
            double gT = 0.0004 * 1.0E4;       
            // Reversal potential for Ca²⁺ in volts
            double eCa = 120.0 * 1.0E-3;   
            // Voltage shift (in mV) used in the equations
            double Vx = 2.0;     

            // Create the IonChannel object with name, conductance, and reversal potential
            IonChannel lowTCalciumChannel = new IonChannel("Low Threshold Calcium Channel", gT, eCa);

            Func<Vector, Vector> alpha_u = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin); // Convert voltage from [V] to [mV]

                // Compute u∞(V)
                Vector uInf = 1.0 / (1.0 + ((Vin + Vx + 81.0) / 4.0).PointwiseExp());
                // Compute τu(V)
                Vector tauU = (30.8 + 211.4 + ((Vin + Vx + 113.2) / 5.0).PointwiseExp())
                            .PointwiseDivide(3.7 * (1.0 + ((Vin + Vx + 84.0) / 3.2).PointwiseExp()));
                // Return alpha_u(V) = u∞(V) / τu(V)
                return uInf.PointwiseDivide(tauU);
            };

            Func<Vector, Vector> beta_u = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);

                // Compute u∞(V)
                Vector uInf = 1.0 / (1.0 + ((Vin + Vx + 81.0) / 4.0).PointwiseExp());
                // Compute τu(V)
                Vector tauU = (30.8 + 211.4 + ((Vin + Vx + 113.2) / 5.0).PointwiseExp())
                            .PointwiseDivide(3.7 * (1.0 + ((Vin + Vx + 84.0) / 3.2).PointwiseExp()));
                // Return beta_u(V) = [1 - u∞(V)] / τu(V)
                return (1.0 - uInf).PointwiseDivide(tauU);
            };


            // Define an instantaneous gating variable "s" that represents s∞(V).
            // For example, we assume:
            //   s∞(V) = 1 / (1 + exp(-(Vin + Vx + 50)/10))
            // This function is computed directly from voltage.
            Func<Vector, Vector> sInf = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin); // Convert to mV
                return 1.0 / (1.0 + (-(Vin + Vx + 57.0) / 6.2).PointwiseExp());
            };

            // Add the gating variable "u" (ODE variable) with exponent 1 and initial probability 0.0.
            lowTCalciumChannel.AddGatingVariable(
                new GatingVariable("u", alpha_u, beta_u, 1, 0.1, nodeCount)
            );

            // Create the instantaneous gating variable "s" with exponent 1.
            // We assume that the GatingVariable class has a Boolean property IsInstant.
            lowTCalciumChannel.AddGatingVariable(
                new GatingVariable("s", sInf, null, 1, 0.1, nodeCount, true)
            );

            return lowTCalciumChannel;
        }
    }
}