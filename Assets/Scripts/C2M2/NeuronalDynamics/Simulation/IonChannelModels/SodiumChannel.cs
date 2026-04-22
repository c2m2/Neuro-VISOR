using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
//using System.Formats.Tar;

namespace C2M2
{
    public class SodiumChannel
    {
        private double gna = 56.0 * 1.0E1;
        private double ena = 50.0 * 1.0E-3;
        //public double vT = 0.0 * 1.0E-3;

        private IonChannel sodium;
        private int nodeCount;
        private double restingV;

        public SodiumChannel (int nodeCount, double restingV)
        {
            this.nodeCount = nodeCount;
            this.restingV = restingV;
            sodium = new IonChannel ("Sodium Channel", gna, ena);
        }

        private Func <Vector, Vector> alpha_m = static voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply (1.0E3, Vin);
            double vT = -50.0;
            return (1.0E3) * (0.32) * (13.0 + vT - Vin).PointwiseDivide(((13.0 + vT - Vin) / 4.0).PointwiseExp() - 1.0);
            //return (1.0E3) * (0.32) * (13.0 + 0 - Vin).PointwiseDivide(((13.0 + 0 - Vin) / 4.0).PointwiseExp() - 1.0);
        
        };

        private Func <Vector, Vector> beta_m = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            double vT = -50.0;
            return (1.0E3) * (0.28) * (Vin - vT - 40.0).PointwiseDivide(((Vin - vT - 40.0) / 5.0).PointwiseExp() - 1.0);
            //return (1.0E3) * (0.28) * (Vin - 0 - 40.0).PointwiseDivide(((Vin - 0 - 40.0) / 5.0).PointwiseExp() - 1.0);
        
        };

        private Func<Vector, Vector> alpha_h = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            double vT = -50.0;
            return (1.0E3) * (0.128) * ((17.0 + vT - Vin) / 18.0).PointwiseExp();
            //return (1.0E3) * (0.128) * ((17.0 + 0 - Vin) / 18.0).PointwiseExp();
        
        };

        private Func<Vector, Vector> beta_h = voltage =>
        {
            //vT = 0;
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            double vT = -50.0;
            return (1.0E3) * 4.0 / (((40.0 + vT - Vin) / 5.0).PointwiseExp() + 1.0);
            //return (1.0E3) * 4.0 / (((40.0 + 0 - Vin) / 5.0).PointwiseExp() + 1.0);
        
        };

        public IonChannel channel()
        {
            IInitializeGate initGate = new IInitializeGate();
            double h_init = initGate.steady_state(alpha_h, beta_h, nodeCount, restingV); // h initial probability
            double m_init = initGate.steady_state(alpha_m, beta_m, nodeCount, restingV); // m initial probability
                
            sodium.AddGatingVariable(
                new GatingVariable("m", alpha_m, beta_m, 3, m_init, nodeCount)
            );
            sodium.AddGatingVariable(
                new GatingVariable("h", alpha_h, beta_h, 1, h_init, nodeCount)
            );

            return sodium;
        }
    }
}