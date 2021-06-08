using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.Adjacency
{
    /// <summary>
    /// stores two vertex indices and an edge length. Used by AdjacencyLisst
    /// </summary>
    public struct Edge
    {
        public int v1 { get; private set; }
        public int v2 { get; private set; }
        public float length { get; private set; }
        public Edge(int v1, int v2, float length)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.length = length;
        }
        public Edge(int v1, int v2, Vector3[] uniqueVerts)
        {
            this.v1 = v1;
            this.v2 = v2;
            length = Vector3.Distance(uniqueVerts[v1], uniqueVerts[v2]);
        }
        public bool Equals(Edge other)
        {
            if ((v1 == other.v2 && v2 == other.v1) || (v1 == other.v1 && v2 == other.v2)) return true;
            else return false;
        }
        public override string ToString() => "(" + v1 + ", " + v2 + "); length: " + length;
    }
}
