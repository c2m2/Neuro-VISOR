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
            double gk = 5.0 * 1.0E1;   
            double ek = -90.0 * 1.0E-3;

            IonChannel potassiumChannel = new IonChannel("Original Potassium Channel", gk, ek);

            Func<Vector, Vector> alpha_n = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin); 
                return (1.0E3) * (0.032) * (15.0 - Vin).PointwiseDivide(((15.0 - Vin) / 5.0).PointwiseExp() - 1.0);
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

        /// <summary>
        /// Sodium channel matching the solver's definitions verbatim
        /// gNa = 60.0e1, eNa = 50.0e-3
        /// alpha_m, beta_m, alpha_h, beta_h exactly as in SparseSolverTestv1
        /// </summary>
        public static IonChannel OriginalSodiumChannel(int nodeCount)
        {
            double gna = 60.0 * 1.0E1;    // 600 S/m²
            double ena = 50.0 * 1.0E-3;   // 0.050 V

            IonChannel sodiumChannel = new IonChannel("Original Sodium Channel", gna, ena);

            // alpha_m(V)
            Func<Vector, Vector> alpha_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.32) * (13.0 - Vin).PointwiseDivide(((13.0 - Vin) / 4.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> beta_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.28) * (Vin - 40.0).PointwiseDivide(((Vin - 40.0) / 5.0).PointwiseExp() - 1.0);
            };

            Func<Vector, Vector> alpha_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.128) * ((17.0 - Vin) / 18.0).PointwiseExp();
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

           


        /// <summary>
        /// Leakage channel matching the solver's gl=0.0, el=-70e-3
        /// </summary>
        
        public static IonChannel OriginalLeakageChannel(int nodeCount)
        {
            double gl = 2.5 * 1.0E-5;
            double el = -70.3 * 1.0E-3;
            return new IonChannel("Original Leakage Channel", gl, el);
        }
            

        /// <summary>
        /// Calcium channel matching the solver's definitions verbatim
        /// gCa = 1.0e1, eCa = 120.0e-3
        /// alpha_q, beta_q, alpha_r, beta_r exactly as in SparseSolverTestv1
        /// </summary>
        public static IonChannel CalciumChannel(int nodeCount)
        {
            double gca = 1.0 * 1.0E1;
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
        /// We use alpha_p(V) = p∞(V)/τp(V), beta_p(V) = [1 - p∞(V)]/τp(V).
        /// Default gM = 0.004 mS/cm² = 0.004 * 10 = 0.04 S/m², τmax = 4.0 s
        /// </summary>
        public static IonChannel SlowPotassiumChannel(int nodeCount)
        {
            // Convert 0.004 mS/cm² to S/m² by multiplying by 10.
            // 0.004 mS/cm² => 0.04 S/m²
            double gM   = 0.04;          // S/m^2
            double eK   = -90.0 * 1.0E-3; // -90 mV in [V]
            double tMax = 4.0;          // 4 s, per Yamada et al.

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
            double gT = 0.05;       
            // Reversal potential for Ca²⁺ in volts
            double eCa = 120.0e-3;   
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

        public static IonChannel NEURONLeakageChannel(int nodeCount)
        {
            double gl = 3 * 1.0E1;           // S/cm^2
            double el = -54.3 * 1.0E-3;     // -54.3 mV converted to V
            return new IonChannel("NEURON Leakage Channel", gl, el);
        }
        /// <summary>
        /// NEURON Sodium Channel with HH kinetics (fast Na+ current).
        /// Equation: I_Na = gNa * m^3 * h * (V - E_Na)
        /// Parameters:
        ///   gNa = 0.30 mS/cm², E_Na = 50 mV (or as used in NEURON)
        /// Rate functions follow classic Hodgkin–Huxley formulas.
        /// </summary>
        public static IonChannel PinkySodiumChannel(int nodeCount)
        {
            double gna = 0.30;         // S/cm²
            double ena = 50.0 * 1e-3;    // V

            IonChannel sodiumChannel = new IonChannel("Pinky Sodium Channel", gna, ena);

            Func<Vector, Vector> alpha_m = voltage =>
            {
                // alpha_m(V) = 1e3 * 0.32*(13 - V(mV)) / (exp((13-V)/4) - 1)
                var Vin = voltage.Clone();
                Vin.Multiply(1e3, Vin);
                // Use a small-value check to avoid division by zero.
                return Vin.Map(v => Math.Abs((13 - v) / 4) > 1e-4
                    ? 1e3 * 0.32 * (13 - v) / (Math.Exp((13 - v) / 4) - 1)
                    : 1e3 * 0.32 * 4);
            };

            Func<Vector, Vector> beta_m = voltage =>
            {
                // beta_m(V) = 1e3 * 0.28*(V - 40) / (exp((V-40)/5) - 1)
                var Vin = voltage.Clone();
                Vin.Multiply(1e3, Vin);
                return Vin.Map(v => Math.Abs((v - 40) / 5) > 1e-4
                    ? 1e3 * 0.28 * (v - 40) / (Math.Exp((v - 40) / 5) - 1)
                    : 1e3 * 0.28 * 5);
            };

            Func<Vector, Vector> alpha_h = voltage =>
            {
                // alpha_h(V) = 1e3 * 0.128 * exp((17 - V)/18)
                var Vin = voltage.Clone();
                Vin.Multiply(1e3, Vin);
                return (1e3 * 0.128) * Vin.Map(v => Math.Exp((17 - v) / 18));
            };

            Func<Vector, Vector> beta_h = voltage =>
            {
                // beta_h(V) = 1e3 * 4.0 / (exp((40 - V)/5) + 1)
                var Vin = voltage.Clone();
                Vin.Multiply(1e3, Vin);
                return (1e3 * 4.0) * Vin.Map(v => 1.0 / (Math.Exp((40 - v) / 5) + 1));
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
        /// NEURON Potassium Channel with HH kinetics (delayed-rectifier K+).
        /// Equation: I_K = gK * n^4 * (V - E_K)
        /// Parameters:
        ///   gK = 0.17 mS/cm², E_K = -107 mV (here, -107e-3 V)
        /// </summary>
        public static IonChannel PinkyPotassiumChannel(int nodeCount)
        {
            double gk = 0.17;             // S/cm²
            double ek = -107.0 * 1e-3;     // V

            IonChannel potassiumChannel = new IonChannel("Pinky Potassium Channel", gk, ek);

            Func<Vector, Vector> alpha_n = voltage =>
            {
                // alpha_n(V) = 1e3 * 0.032*(15 - V) / (exp((15 - V)/5) - 1)
                var Vin = voltage.Clone();
                Vin.Multiply(1e3, Vin);
                return Vin.Map(v => Math.Abs((15 - v) / 5) > 1e-4
                    ? 1e3 * 0.032 * (15 - v) / (Math.Exp((15 - v) / 5) - 1)
                    : 1e3 * 0.032 * 5);
            };

            Func<Vector, Vector> beta_n = voltage =>
            {
                // beta_n(V) = 1e3 * 0.5 * exp((10 - V)/40)
                var Vin = voltage.Clone();
                Vin.Multiply(1e3, Vin);
                return Vin.Map(v => 1e3 * 0.5 * Math.Exp((10 - v) / 40));
            };

            potassiumChannel.AddGatingVariable(
                new GatingVariable("n", alpha_n, beta_n, 4, 0.0376969, nodeCount)
            );

            return potassiumChannel;
        }

        /// <summary>
        /// NEURON Leakage Channel.
        /// Equation: I_L = gL * (V - E_L)
        /// Parameters:
        ///   gL = 0.05 mS/cm², E_L = -65 mV (converted to -65e-3 V)
        /// </summary>
        public static IonChannel PinkyLeakageChannel(int nodeCount)
        {
            double gl = 0.05;            // S/cm²
            double el = -65.0 * 1e-3;      // V

            return new IonChannel("Pinky Leakage Channel", gl, el);
        }

        /// <summary>
        /// NEURON Calcium Channel (high-threshold, L-type).
        /// Equation: I_Ca = gCa * m^2 * (V - E_Ca)
        /// Parameters:
        ///   gCa = 0.06 mS/cm², E_Ca = 125 mV (converted to 125e-3 V)
        /// Note: In the mod file the gating variable m has exponent 2.
        /// </summary>
        public static IonChannel PinkyCalciumChannel(int nodeCount)
        {
            double gca = 0.06;           // S/cm²
            double eca = 125.0 * 1e-3;     // V

            IonChannel calciumChannel = new IonChannel("Pinky Calcium Channel", gca, eca);

            // For simplicity we use a sigmoidal steady state and constant time constant:
            Func<Vector, Vector> m_inf = voltage =>
            {
                // m_inf(V) = 1 / (1 + exp((V + 10)/-10))
                return voltage.Map(v => 1.0 / (1 + Math.Exp((v + 10) / -10)));
            };

            // Here we choose a fixed tau_m (e.g., 5 ms)
            Func<Vector, Vector> tau_m = voltage =>
            {
                return Vector.Build.Dense(voltage.Count, 5.0);
            };

            calciumChannel.AddGatingVariable(
                new GatingVariable("m", m_inf, tau_m, 2, 0.0, nodeCount)
            );

            return calciumChannel;
        }

        /// <summary>
        /// NEURON Ca²⁺-activated K⁺ Channel.
        /// Equation: I_KCa = gKCa * m^3 * (V - E_K)
        /// Parameters:
        ///   gKCa = 0.15 mS/cm², E_K = -107 mV (converted to -107e-3 V)
        /// For simplicity, we use a voltage-independent activation based on [Ca²⁺] (here approximated by a sigmoidal function).
        /// </summary>
        public static IonChannel PinkyKCaChannel(int nodeCount)
        {
            double gkca = 0.15;           // S/cm²
            double ek = -107.0 * 1e-3;      // V

            IonChannel kCaChannel = new IonChannel("Pinky KCa Channel", gkca, ek);

            // For this example we assume a simple sigmoidal dependence on calcium.
            // In practice you would base this on intracellular calcium concentration.
            // Here we simply use a dummy voltage dependence as a placeholder.
            Func<Vector, Vector> m_inf = voltage =>
            {
                // Example: m_inf(V) = 1/(1+exp(-(V+30)/5))
                return voltage.Map(v => 1.0 / (1 + Math.Exp(-(v + 30) / 5)));
            };

            Func<Vector, Vector> tau_m = voltage =>
            {
                // Fixed time constant (e.g., 10 ms)
                return Vector.Build.Dense(voltage.Count, 10.0);
            };

            kCaChannel.AddGatingVariable(
                new GatingVariable("m", m_inf, tau_m, 3, 0.0, nodeCount)
            );

            return kCaChannel;
        }

    }
}
