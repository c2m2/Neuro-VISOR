using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace C2M2
{
    public class PotassiumChannel
    {
        private double gk = 6.0 * 1.0E1;
        private double ek = -90.0 * 1.0E-3;
        
        private IonChannel potassium;
        
        public PotassiumChannel ()
        {
            potassium = new IonChannel ("Potassium Channel", gk, ek);
        }

        public Func <Vector, Vector> alpha_n = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply (1.0E3, Vin);
            double vT = -50.0;
            return (1.0E3) * (0.032) * (15.0 + vT - Vin).PointwiseDivide(((15.0 + vT - Vin) / 5.0).PointwiseExp() - 1.0);
            //return (1.0E3) * (0.032) * (15.0 + 0 - Vin).PointwiseDivide(((15.0 + 0 - Vin) / 5.0).PointwiseExp() - 1.0);
        };

        public Func <Vector, Vector> beta_n = voltage =>
        {
            var Vin = voltage.Clone();
            Vin.Multiply(1.0E3, Vin);
            double vT = -50.0;
            return (1.0E3) * (0.5) * ((10.0 + vT - Vin) / 40.0).PointwiseExp();
            //return (1.0E3) * (0.5) * ((10.0 + 0 - Vin) / 40.0).PointwiseExp();
        };

        
        public IonChannel channel()
        {
            /*
            potassium.AddGatingVariable(
                new GatingVariable ("n", alpha_n, beta_n, 4, 0.03769685637004722, nodeCount)
            );
            */
            return potassium;
        }
        

        public double total_conductance()
        {
            return 0;
        }
    }
}