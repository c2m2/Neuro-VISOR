using System;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MathNet.Numerics.LinearAlgebra;

namespace C2M2.NeuronalDynamics.Simulation
{
    /// <summary>
    /// This file consists of the channels for each Ion Channel. The equations follow the class architecture listed in IonChannels.cs
    /// </summary>
    public static class IonChannelModels
    {
        /// <summary>
        /// Potassium Channel
        /// gK = 5.0e1, eK = -90e-3
        /// Rate equations taken from Pospischil et al., 2008
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
            /// Declare new ion channel, "potassiumChannel"
            IonChannel potassiumChannel = new IonChannel("Potassium Channel", gk, ek);
            /// <summary>
            /// This is \f$\alpha_n\f$ rate function, the rate functions take the form of
            /// αn = −0.032(V − VT −15) / exp[−(V − VT − 15)/5] − 1
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>alpha_n</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> alpha_n = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin); 
                return (1.0E3) * (0.032) * (15.0 + vT - Vin).PointwiseDivide(((15.0 + vT - Vin) / 5.0).PointwiseExp() - 1.0);
            };
            /// <summary>
            /// This is \f$\beta_n\f$ rate function, the rate functions take the form of
            /// βn = 0.5 exp[−(V − VT − 10)/40],
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>beta_n</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> beta_n = voltage =>
            {
                var Vin = voltage.Clone();
                // Convert voltage from [V] to [mV]
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.5) * ((10.0 + vT - Vin) / 40.0).PointwiseExp();
            };
            /// Format for returning gating variables
            /// new GatingVariable("variable name", alpha_function, beta_function, exponenet, initial probability, nodeCount)
            potassiumChannel.AddGatingVariable(
                new GatingVariable("n", alpha_n, beta_n, 4, 0.0376969, nodeCount)
            );
            return potassiumChannel;
        }

        /// <summary>
        /// Sodium Channel
        /// gNa = 50.0e1, eNa = 50.0e-3
        /// Rate equations taken from Pospischil et al., 2008
        /// </summary>
        public static IonChannel SodiumChannel(int nodeCount)
        {
            /// <summary>
            /// [S/m2] sodium conductance per unit area, this is the Sodium conductance per unit area, it is used in this term
            /// INa = ḡNa m^3h (V − enaa)
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
            /// Declaration of new ion channel sodiumChannel.
            /// Declared with the struct: (Name, Coductance, Reversal)
            IonChannel sodiumChannel = new IonChannel("Sodium Channel", gna, ena);
            /// <summary>
            /// This is \f$\alpha_m\f$ rate function, the rate functions take the form of
            /// αm = −0.32(V − VT −13) / exp[−(V − VT − 13)/4] − 1
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>alpha_m</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> alpha_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.32) * (13.0 + vT - Vin).PointwiseDivide(((13.0 + vT - Vin) / 4.0).PointwiseExp() - 1.0);
            };
            /// <summary>
            /// This is \f$\beta_m\f$ rate function, the rate functions take the form of
            /// βm = 0.28(V − VT −40) / exp[(V − VT − 40)/5] − 1
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>beta_m</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> beta_m = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.28) * (Vin - vT - 40.0).PointwiseDivide(((Vin - vT - 40.0) / 5.0).PointwiseExp() - 1.0);
            };
            /// <summary>
            /// This is \f$\alpha_h\f$ rate function, the rate functions take the form of
            /// αh = 0.128 exp[−(V − VT − 17)/18]
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>alpha_h</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> alpha_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.128) * ((17.0 + vT - Vin) / 18.0).PointwiseExp();
            };
            /// <summary>
            /// This is \f$\beta_h\f$ rate function, the rate functions take the form of
            /// βh=4 / 1 + exp[−(V − VT − 40)/5]
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>beta_h</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> beta_h = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * 4.0 / (((40.0 + vT - Vin) / 5.0).PointwiseExp() + 1.0);
            };
            /// Format for returning gating variables
            /// new GatingVariable("variable name", alpha_function, beta_function, exponenet, initial probability, nodeCount)
            sodiumChannel.AddGatingVariable(
                new GatingVariable("m", alpha_m, beta_m, 3, 0.0147567, nodeCount)
            );
            sodiumChannel.AddGatingVariable(
                new GatingVariable("h", alpha_h, beta_h, 1, 0.9959410, nodeCount)
            );

            return sodiumChannel;
        }

        /// <summary>
        /// Leakage Channel
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
            double el = -70.0 * 1.0E-3;
            return new IonChannel("Leakage Channel", gl, el);
        }
            

        /// <summary>
        /// Calcium Channel
        /// gCa = 0.0001e4, eCa = 120.0e-3
        /// Rate equations taken from Pospischil et al., 2008
        /// Conductance taken from Fig. 5, Model of intrinsically bursting cell
        /// </summary>
        public static IonChannel CalciumChannel(int nodeCount)
        {
            double gca = 0.0001 * 1.0E4;
            /// <summary>
            /// [V] calcium reversal potential
            /// </summary>
            double eca = 120.0 * 1.0E-3;

            IonChannel calciumChannel = new IonChannel("Calcium Channel", gca, eca);
            /// <summary>
            /// This is \f$\alpha_q\f$ rate function, the rate functions take the form of
            /// αq = 0.055(−27 − V) / exp[(−27 − V )/3.8] − 1
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>alpha_q</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> alpha_q = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.055) * (27.0 - Vin) / (((-27.0 - Vin) / 3.8).PointwiseExp() - 1.0);
            };
            /// <summary>
            /// This is \f$\beta_q\f$ rate function, the rate functions take the form of
            /// βq = 0.94 exp[(−75 − V) / 17]
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>beta_q</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> beta_q = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.94) * (((-75.0 - Vin) / 17.0).PointwiseExp());
            };
            /// <summary>
            /// This is \f$\alpha_r\f$ rate function, the rate functions take the form of
            /// αr = 0.000457 exp[(−13 − V )/50]
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>alpha_r</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> alpha_r = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.000457) * (((-13.0 - Vin) / 50.0).PointwiseExp());
            };
            /// <summary>
            /// This is \f$\beta_r\f$ rate function, the rate functions take the form of
            /// βr = 0.0065 / exp[(−15 − V )/28] + 1
            /// </summary>
            /// <param name="voltage"></param> this is the input voltage
            /// <returns>beta_r</returns> this function returns the rate at the given voltage
            Func<Vector, Vector> beta_r = voltage =>
            {
                var Vin = voltage.Clone();
                Vin.Multiply(1.0E3, Vin);
                return (1.0E3) * (0.0065) / (((-15.0 - Vin) / 28.0).PointwiseExp() + 1.0);
            };
            /// Format for returning gating variables
            /// new GatingVariable("variable name", alpha_function, beta_function, exponenet, initial probability, nodeCount)
            calciumChannel.AddGatingVariable(
                new GatingVariable("q", alpha_q, beta_q, 2, 0.0, nodeCount)
            );
            calciumChannel.AddGatingVariable(
                new GatingVariable("r", alpha_r, beta_r, 1, 0.0, nodeCount)
            );

            return calciumChannel;
        }

        /// <summary>
        /// Slow Potassium Channel
        ///   p∞(V) = 1 / [1 + exp(-(V+35)/10)]
        ///   τp(V) = τmax / [3.3 * exp((V+35)/20) + exp(-(V+35)/20)]
        ///   dp/dt = ( p∞(V) - p ) / τp(V)
        /// We use alpha_p(V) = p∞(V/)τp(V), beta_p(V) = [1 - p∞(V)]/τp(V).
        /// Default gM = 0.004 mS/cm² = 0.004 * 10 = 0.04 S/m², τmax = 4.0 s
        /// </summary>
        public static IonChannel SlowPotassiumChannel(int nodeCount)
        {
            // [S/m²] slow sotassium conductance
            double gM   = 7.5E-5 * 1.0E4;
            /// <summary>
            /// [V] slow potassium reversal potential
            /// </summary>
            double eK   = -90.0 * 1.0E-3;
            /// <summary>
            /// [S] maximum time constant, tmax, for the p-gate.
            /// </summary>
            double tMax = .400;

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

            // Add the gating variable 'p' with exponent = 1 and initial probability 0.0
            // To do: set initial probabilities through numerical means
            slowKChannel.AddGatingVariable(
                new GatingVariable("p", alpha_p, beta_p, 1, 0.0, nodeCount)
            );

            return slowKChannel;
        }

        /// <summary>
        /// Low Threshold Calcium Channel (IT):
        /// 
        /// IT = gT * [ s∞(V) ]^2 * u * (V - Eca)
        ///
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
        /// </summary>
        public static IonChannel LowThresholdCalciumChannel(int nodeCount)
        {
            // [S/m²] Low Threshold Calcium Conductance
            double gT = 0.0004 * 1.0E4;       
            // Reversal potential for Ca²⁺ in volts
            double eCa = 120.0 * 1.0E-3;   
            // Voltage shift (in mV)
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

                Vector uInf = 1.0 / (1.0 + ((Vin + Vx + 81.0) / 4.0).PointwiseExp());
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
                Vin.Multiply(1.0E3, Vin);
                return 1.0 / (1.0 + (-(Vin + Vx + 57.0) / 6.2).PointwiseExp());
            };

            // Add the gating variable "u" with exponent 1 and initial probability 0.0
            lowTCalciumChannel.AddGatingVariable(
                new GatingVariable("u", alpha_u, beta_u, 1, 0.0, nodeCount)
            );

            // Create the instantaneous gating variable "s" with exponent 1.
            // The instantaneous variable is set via "true" as the last parameter passed
            lowTCalciumChannel.AddGatingVariable(
                new GatingVariable("s", sInf, null, 1, 0.0, nodeCount, true)
            );

            return lowTCalciumChannel;
        }
    }
}