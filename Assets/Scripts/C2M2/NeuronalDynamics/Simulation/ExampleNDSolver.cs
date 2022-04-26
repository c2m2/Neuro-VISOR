using System;
using System.Collections.Generic;

namespace C2M2.NeuronalDynamics.Simulation
{
    public class ExampleNDSolver : NDSimulation
    {
        // One value for each 1D vertex
        double[] vals;
        double[] vals_active;

        public override double[] Get1DValues()
        {
            lock (visualizationValuesLock) return vals;
        }
        public override void Set1DValues((int, double)[] newValues)
        {
            foreach ((int, double) val in newValues)
            {
                int index = val.Item1;
                double value = val.Item2;
                vals_active[index] = value;
            }
        }

        internal override void SetSynapseCurrent(List<(Synapse, Synapse)> newValues)
        {
            foreach ((Synapse, Synapse) val in newValues)
            {
                int index = val.Item1.FocusVert;
                double value = val.Item2.simulation.Get1DValues()[val.Item2.FocusVert];
                vals_active[index] = value;
            }
        }

        double diffusionConst = 0.05f;
        protected override void PreSolve()
        {
            lock (visualizationValuesLock) vals = new double[Neuron.nodes.Count];
            vals_active = new double[Neuron.nodes.Count];
        }
        protected override void SolveStep(int t)
        {
            for (int i = 0; i < Neuron.nodes.Count; i++)
            {
                foreach(var n in Neuron.nodes[i].Neighbors)
                {
                    double diffusionAmt = diffusionConst * vals_active[n.id];
                    vals_active[i] += diffusionAmt;
                    vals_active[n.id] -= diffusionAmt;
                }
            }

            // Remove value from dendrite caps
            foreach(var node in Neuron.boundaryNodes)
            {
                vals_active[node.Id] -= diffusionConst * vals_active[node.Id];
            }
            
        }

        internal override void SetOutputValues()
        {
            lock (visualizationValuesLock) vals = (double[])vals_active.Clone();
        }

    }
}