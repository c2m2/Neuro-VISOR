using System;
using System.Collections.Generic;

namespace C2M2.NeuronalDynamics.Alg
{
    /// <summary>
    /// Very simple class representing a graph consisting of vertices and edges of a given type T
    /// </summary>
    /// <typeparam="T"> vertex type </typeparam>
    public class Graph<T>
    {
        public Graph() { }
        /// <summary>
        /// Construct a graph out of a list of vertices and edges
        /// </summary>
        public Graph(IEnumerable<T> vertices, IEnumerable<Tuple<T, T>> edges)
        {
            foreach (var vertex in vertices)
                AddVertex(vertex);

            foreach (var edge in edges)
                AddEdge(edge);
        }

        /// Store adjacency list
        public Dictionary<T, HashSet<T>> AdjacencyList { get; } = new Dictionary<T, HashSet<T>>();

        /// <summary>
        /// Add a vertex to the graph
        /// </summary>
        /// <param name="vertex"> A vertex </param>
        public void AddVertex(T vertex)
        {
            AdjacencyList[vertex] = new HashSet<T>();
        }
        
        /// <summary>
        /// Add an edge to the graph
        /// </summary>
        /// <param name="edge"> edge </param>
        public void AddEdge(Tuple<T, T> edge)
        {
            if (AdjacencyList.ContainsKey(edge.Item1) && AdjacencyList.ContainsKey(edge.Item2))
            {
                AdjacencyList[edge.Item1].Add(edge.Item2);
                AdjacencyList[edge.Item2].Add(edge.Item1);
            }
        }
    }

    /// <summary>
    /// Algorithmic utilities
    /// </summary>
    public sealed class Algorithms
    {
        /// <summary>
        /// DFS search on the graph with start vertex T
        /// </summary>
        /// <param name="graph"> A graph </param>
        /// <param naem="start"> Start vertex </param>
        public HashSet<T> DFS<T>(Graph<T> graph, T start)
        {
            var visited = new HashSet<T>();

            if (!graph.AdjacencyList.ContainsKey(start))
                return visited;

            var stack = new Stack<T>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var vertex = stack.Pop();

                if (visited.Contains(vertex))
                    continue;

                visited.Add(vertex);

                foreach (var neighbor in graph.AdjacencyList[vertex])
                    if (!visited.Contains(neighbor))
                        stack.Push(neighbor);
            }

            return visited;
        }
    }
}
