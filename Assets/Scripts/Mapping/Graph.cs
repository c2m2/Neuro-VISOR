using System;
using System.Collections.Generic;

namespace C2M2 {
  namespace ALG {
    /// <summary>
    /// Very simple class graph
    /// </summary>
    public class Graph<T> {
        public Graph() {}
        public Graph(IEnumerable<T> vertices, IEnumerable<Tuple<T,T>> edges) {
            foreach(var vertex in vertices)
                AddVertex(vertex);

            foreach(var edge in edges)
                AddEdge(edge);
        }

        public Dictionary<T, HashSet<T>> AdjacencyList { get; } = new Dictionary<T, HashSet<T>>();

        public void AddVertex(T vertex) {
            AdjacencyList[vertex] = new HashSet<T>();
        }

        public void AddEdge(Tuple<T,T> edge) {
            if (AdjacencyList.ContainsKey(edge.Item1) && AdjacencyList.ContainsKey(edge.Item2)) {
                AdjacencyList[edge.Item1].Add(edge.Item2);
                AdjacencyList[edge.Item2].Add(edge.Item1);
            }
        }
    }

    /// <summary>
    /// Algorithmic utilities
    /// </summary>
    public sealed class Algorithms {
        public HashSet<T> DFS<T>(Graph<T> graph, T start) {
            var visited = new HashSet<T>();

            if (!graph.AdjacencyList.ContainsKey(start))
                return visited;
                
            var stack = new Stack<T>();
            stack.Push(start);

            while (stack.Count > 0) {
                var vertex = stack.Pop();

                if (visited.Contains(vertex))
                    continue;

                visited.Add(vertex);

                foreach(var neighbor in graph.AdjacencyList[vertex])
                    if (!visited.Contains(neighbor))
                        stack.Push(neighbor);
            }

            return visited;
	  }
        }
    }
}