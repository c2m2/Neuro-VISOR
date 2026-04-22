using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
//using System.Formats.Tar;

namespace C2M2
{
    public class CalciumChannel
    {
        private double gca = 1.0;
        private double eca = 120.0 * 1.0E-3;
        
        private IonChannel calcium;
        private int nodeCount;
        private double restingV;

        public CalciumChannel (int nodeCount, double restingV)
        {
            this.nodeCount = nodeCount;
            this.restingV = restingV;
            calcium = new IonChannel ("Calcium Channel", gca, eca);
        }

        private Func <Vector, Vector> alpha_q = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply (1.0E3, Vin);

            return (1.0E3) * (0.055) * (-27.0 - Vin) / (((-27.0 - Vin) / 3.8).PointwiseExp() - 1.0);        
        };

        private Func <Vector, Vector> beta_q = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.94) * (((-75.0 - Vin) / 17.0).PointwiseExp());
        };

        private Func<Vector, Vector> alpha_r = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.000457) * (((-13.0 - Vin) / 50.0).PointwiseExp());        
        };

        private Func<Vector, Vector> beta_r = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            return (1.0E3) * (0.0065) / (((-15.0 - Vin) / 28.0).PointwiseExp() + 1.0);
        
        };

        public IonChannel channel()
        {
            IInitializeGate initGate = new IInitializeGate();
            double q_init = initGate.steady_state(alpha_q, beta_q, nodeCount, restingV); // q initial probability
            double r_init = initGate.steady_state(alpha_r, beta_r, nodeCount, restingV); // m initial probability
                
            calcium.AddGatingVariable(
                new GatingVariable("q", alpha_q, beta_q, 2, q_init, nodeCount)
            );
            calcium.AddGatingVariable(
                new GatingVariable("r", alpha_r, beta_r, 1, r_init, nodeCount)
            );

            return calcium;
        }
    }
}