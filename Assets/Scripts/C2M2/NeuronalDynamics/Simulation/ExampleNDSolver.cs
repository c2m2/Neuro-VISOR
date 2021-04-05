using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Simulation
{
    public class ExampleNDSolver : NDSimulation
    {
        // One value for each 1D vertex
        double[] vals;

        public override double Get1DValue(int index1D)
        {
            return vals[index1D];
        }
        public override double[] Get1DValues()
        {
            return vals;
        }
        public override void Set1DValues(Tuple<int, double>[] newValues)
        {
            foreach(Tuple<int, double> val in newValues)
            {
                int index = val.Item1;
                double value = val.Item2;
                vals[index] = value;
            }
        }
        double diffusionConst = 0.05f;
        protected override void PreSolve()
        {
            vals = new double[Neuron.nodes.Count];
        }
        protected override void SolveStep(int t)
        {
            for (int i = 0; i < Neuron.nodes.Count; i++)
            {
                foreach(var n in Neuron.nodes[i].Neighbors)
                {
                    double diffusionAmt = diffusionConst * vals[n.id];
                    vals[i] += diffusionAmt;
                    vals[n.id] -= diffusionAmt;
                }
            }

            // Remove value from dendrite caps
            foreach(var node in Neuron.boundaryNodes)
            {
                vals[node.Id] -= diffusionConst * vals[node.Id];
            }
            
        }
    }
}