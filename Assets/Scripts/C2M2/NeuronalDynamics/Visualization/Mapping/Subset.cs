using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace C2M2.NeuronalDynamics.UGX
{
    /// <summary>
    /// Wrapper class for subset information 
    /// </summary>
    public class SubsetCollection
    {
        public Dictionary<string, Subset> subsets = new Dictionary<string, Subset>();
        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="name"> Name of list entry to index </param>
        public Subset this[string name]
        {
            get => (Subset)subsets[name];
            set => subsets[name] = value;
        }

        /// <summary>
        /// Print out all subset information in the current subset collection for a grid
        /// </summary>
        /// <returns> string </returns>
        /// Returns the string representation
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var entry in subsets)
            {
                builder.Append(entry.Value);
                builder.AppendLine();
            }
            return builder.ToString();
        }
    }


    /// <summary>
    /// A simple data class representing Subset information
    /// </summary>
    public readonly struct Subset
    {
        public string Name { get; }
        public int[] Indices { get; }
        /// <summary>
        /// Construct a subset information with a name and corresponding indices
        /// </summary>
        /// <param name="name"> Name of subset </param>
        /// <param name="indices"> Indices of the subset </param>
        public Subset(in string name, in int[] indices)
        {
            Name = name;
            Indices = indices;
        }

        /// <summary>
        /// Print out information for this subset, i.e. name and indices as a list
        /// </summary>
        /// <returns> string </returns>
        /// Returns the string representation
        public override string ToString() => return $"Subset >>{Name}<< has vertex indices >>{string.Join(", ", Indices)}<<";
    }
}
