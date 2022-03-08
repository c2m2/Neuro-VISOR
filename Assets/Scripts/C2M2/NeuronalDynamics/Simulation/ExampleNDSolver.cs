﻿using System;
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
        public override void Set1DValues(Tuple<int, double>[] newValues)
        {
            foreach(Tuple<int, double> val in newValues)
            {
                int index = val.Item1;
                double value = val.Item2;
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

        internal override void HandleSynapses(List<(Synapse, Synapse)> synapses)
        {
            Tuple<int, double>[] new1DVoltages = new Tuple<int, double>[synapses.Count];

            // apply the voltage from the pre-synapse and to the location of the postsynapse
            for (int i = 0; i < synapses.Count; i++)
            {
                new1DVoltages[i] = new Tuple<int, double>(synapses[i].Item2.nodeIndex, synapses[i].Item1.attachedSim.Get1DValues()[synapses[i].Item1.nodeIndex]);
            }

            // Pass the tuple so we can set our new voltage value
            Set1DValues(new1DVoltages);
        }
    }
}