using System;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra;

namespace C2M2.NeuronalDynamics.Simulation
{
    public static class PremadeChannels
    {
        /// <summary>
        /// Potassium channel matching the solver's definitions verbatim
        /// gK = 5.0e1, eK = -90e-3
        /// alpha_n, beta_n exactly as in SparseSolverTestv1
        /// </summary>
        public static IonChannel OriginalPotassiumChannel(int nodeCount)
        {
            double gk = 0.01 * 1.0E4;     // => 100 S/m²
            double ek = -100.0 * 1.0E-3;  // => -100 V
            double vT = -55.0;
            // double vT = 0.0;

            IonChannel potassiumChannel = new IonChannel("Original Potassium Channel", gk, ek);

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
        /// Sodium channel matching the solver's definitions verbatim
        /// gNa = 60.0e1, eNa = 50.0e-3
        /// alpha_m, beta_m, alpha_h, beta_h exactly as in SparseSolverTestv1
        /// </summary>
        public static IonChannel OriginalSodiumChannel(int nodeCount)
        {
            double gna = 0.05 * 1.0E4;    // S/m2
            double ena = 50.0 * 1.0E-3;
            double vT = -55.0;
            // double vT = 0.0;

            IonChannel sodiumChannel = new IonChannel("Original Sodium Channel", gna, ena);

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
        /// Leakage channel matching the solver's gl=0.0, el=-70e-3
        /// </summary>
        
        public static IonChannel OriginalLeakageChannel(int nodeCount)
        {
            double gl = 1.5E-4 * 1.0E4;  // S/m²
            double el = -70.0 * 1.0E-3;   // mV
            return new IonChannel("Original Leakage Channel", gl, el);
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




        /// <summary>
        /// Chloride channel matching the solver's definitions verbatim
        /// user can pass gCl, eCl
        /// alpha_cl, beta_cl as in SparseSolverTestv1
        /// </summary>
        // public static IonChannel ChlorideChannel(int nodeCount, double gcl, double ecl)
        // {
        //     IonChannel chlorideChannel = new IonChannel("Chloride Channel", gcl, ecl);

        //     Func<Vector, Vector> alpha_cl = voltage =>
        //     {
        //         var Vin = voltage.Clone();
        //         Vin.Multiply(1.0E3, Vin);
        //         return (1.0E3 * 0.07)
        //             * (Vin.Add(20.0))
        //             .PointwiseDivide(
        //                 (Vin.Add(20.0).Divide(10.0))
        //                 .PointwiseExp()
        //                 .Subtract(1.0)
        //             );
        //     };

        //     Func<Vector, Vector> beta_cl = voltage =>
        //     {
        //         var Vin = voltage.Clone();
        //         Vin.Multiply(1.0E3, Vin);
        //         return (1.0E3 * 0.1)
        //             * (-(Vin.Add(30.0)).Divide(10.0))
        //             .PointwiseExp();
        //     };

        //     chlorideChannel.AddGatingVariable(
        //         new GatingVariable("x", alpha_cl, beta_cl, 1, 0.5, nodeCount)
        //     );

        //     return chlorideChannel;
        // }

        /// <summary>
        /// NEURON Sodium Channel with HH defaults.
        /// </summary>
        /// <summary>
        /// NEURON Sodium Channel with HH defaults (Yale Neuron parameters):
        /// gnabar = 0.12 S/cm^2, ena = 50 mV.
        /// Gating: m^3 * h.
        /// </summary>
     
        public static IonChannel NEURONPotassiumChannel(int nodeCount)
        {
            double gk = 360 * 1.0E1;          // S/cm^2
            double ek = -77.0 * 1.0E-3;   // -77 mV converted to V
            IonChannel potassiumChannel = new IonChannel("NEURON Potassium Channel", gk, ek);

            Func<Vector, Vector> alpha_n = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.032) * (15.0 - Vin)
                    .PointwiseDivide(((15.0 - Vin) / 5.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> beta_n = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.5) * ((10.0 - Vin) / 40.0).PointwiseExp();
            };

            potassiumChannel.AddGatingVariable(
                new GatingVariable("n", alpha_n, beta_n, 4, 0.0376969, nodeCount)
            );

            return potassiumChannel;
        }
        public static IonChannel NEURONSodiumChannel(int nodeCount)
        {
            double gna = 120 * 1.0E1;          // S/cm^2
            double ena = 50.0 * 1.0E-3;   // 50 mV converted to V
            IonChannel sodiumChannel = new IonChannel("NEURON Sodium Channel", gna, ena);
            // Use the HH kinetics (m^3 * h) with standard HH rate functions:
            // [Define alpha_m, beta_m, alpha_h, beta_h based on the standard HH model; see the NEURON manual (Hodgkin & Huxley, 1952) or the NEURON documentation]
            // For brevity, here is a simplified version:
            Func<Vector, Vector> alpha_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.32) * (13.0 - Vin)
                    .PointwiseDivide(((13.0 - Vin) / 4.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> beta_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.28) * (Vin - 40.0)
                    .PointwiseDivide(((Vin - 40.0) / 5.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> alpha_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.128) * ((17.0 - Vin) / 18.0)
                    .PointwiseExp();
            };

            Func<Vector, Vector> beta_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * 4.0 / (((40.0 - Vin) / 5.0).PointwiseExp() + 1.0);
            };

            sodiumChannel.AddGatingVariable(
                new GatingVariable("m", alpha_m, beta_m, 3, 0.0147567, nodeCount)
            );
            sodiumChannel.AddGatingVariable(
                new GatingVariable("h", alpha_h, beta_h, 1, 0.9959410, nodeCount)
            );

            return sodiumChannel;
        }
    }
}