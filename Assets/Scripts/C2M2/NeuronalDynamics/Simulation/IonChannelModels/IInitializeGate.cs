using UnityEngine;
using System;
using MathNet.Numerics.LinearAlgebra;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using System.ComponentModel;

public class IInitializeGate
{
    public double steady_state (
        Func <Vector, Vector> alpha,
        Func <Vector, Vector> beta,
        int nodeCount, 
        double voltage)
    {
        if (alpha == null) return 0.0;
        
        var v = Vector.Build.Dense (nodeCount, voltage);

        double a = alpha(v)[0];
        double b = 0.0;
        if (beta != null) b = beta(v)[0];

        return a / (a + b);
    }

}