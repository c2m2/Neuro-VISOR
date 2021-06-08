using System;
namespace C2M2.Interaction.Adjacency
{
    /// <summary>
    /// Neighboring vertex/graph of the mesh
    /// </summary>
    public struct Node : IComparable<Node>
    {
        public float weight { get; set; }
        public int index { get; set; }
        /// <summary> Construct a neighbor </summary>
        /// <param name="weight"> Weight to neighbor </param>
        /// <param name="index"> Index of neighbor </param>
        public Node(float weight, int index)
        {
            this.weight = weight;
            this.index = index;
        }
        /// <summary> Compare two neighbors by weight. <see cref="IComparable<T>"/> </summary>
        /// <param name="other"> Neighbor to compare to </param>
        /// <returns> 1 if this node has higher weight than node "other", 
        /// -1 if this node has lower weight, 
        /// 0 if weights are equal </returns>
        /// TODO: This isn't safe code, since it outright compares floats. It might be more accurate as some form of:
        /// 
        /// double difference = weight - other.weight;
        /// double epsilon = 1.19e-7f * Max(weight, other.weight);
        // if(diffrence > epsilon) return 1; (this is sufficiently larger than other)
        // else if(difference < -epsilon) return -1; (other is sufficiently larger than this)
        // else return 0 (-epsilon < difference < epsilon, so this is close enough to other to be considered equal.
        //
        // Consider keeping a running total of the highest weight node among all nodes. Then you could have a global epsilon = 1.19e-7f * MaxNode.weight and save some computation later
        public int CompareTo(Node other)
        {
            if (weight < other.weight) return -1;
            else if (weight > other.weight) return 1;
            else return 0;
        }
        /// <summary> Represent a neighbor as a string </summary>
        /// <returns> string representation </returns>
        public override string ToString()
        {
            return "(" + index + ", " + weight + ")";
        }
    }
}