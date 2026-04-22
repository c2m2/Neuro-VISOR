using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace C2M2
{
    public class LeakageChannel
    {
        private double gl = 0.1 * 1.0E-4;
        private double el = -70.0 * 1.0E-3;

        private IonChannel leakage;
        private int nodeCount;
        private double restingV;

        public LeakageChannel (int nodeCount, double restingV)
        {
            this.nodeCount = nodeCount;
            this.restingV = restingV;
            leakage = new IonChannel ("Leakage Channel", gl, el);
        }

        public IonChannel channel()
        {
            return leakage;
        }
    }
}