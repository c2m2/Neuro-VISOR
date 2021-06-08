using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace C2M2.Interaction.Adjacency
{
    using Utils;
    /// <summary>
    /// Runs a dijkstra search over a surface of mesh vertices, given an initial hit point and a distance threshold
    /// </summary>
    public class DijkstraSearch : MonoBehaviour
    {
        /// <summary> Stores a list of all mesh and invisible vertices that need to be traversed </summary>
        private UniqueVertices uniqueVertices = null;
        /// <summary> Stores the distances between adjacent real and invisible vertices </summary>
        private AdjacencyList adjacencyList = null;
        public float[] minDistances { get; private set; } = null;
        private void Awake()
        {
            adjacencyList = GetComponent<AdjacencyList>() ?? gameObject.AddComponent<AdjacencyList>();
            uniqueVertices = GetComponent<UniqueVertices>() ?? gameObject.AddComponent<UniqueVertices>();
            minDistances = new float[uniqueVertices.uniqueVerts.Length];
        }
        /// <summary> Finds the shortest path to any node in the graph starting from initialVertIndex below threshold </summary>
        public int[] Search(RaycastHit hit, float distanceThreshold)
        {
            // Create an array to store distances
            minDistances = new float[uniqueVertices.uniqueVerts.Length];
            // Initialize min distance array to be all Infinity
            minDistances.Fill(float.PositiveInfinity);
            // Find the vertices adjacent to our raycast hit to start the search from
            List<Node> initialNodes = uniqueVertices.RaycastFindNearestUniqueVerts(hit, 1);
            // Create a priority queue to queue the next adjacent nodes
            PriorityQueue queue = new PriorityQueue();
            // Our raycast hits in between three nodes, so we find the distance between our raycast hit point and those nodes, and then queue all of them as our "initial" nodes
            for (int i = 0; i < initialNodes.Count; i++)
            { // Queue each initial node and store its initial weight in the min_distances array
                queue.Enqueue(initialNodes[i]);
                minDistances[initialNodes[i].index] = initialNodes[i].weight;
            }
            // Store a list of the nearest mesh vertex neighbors to our point
            List<int> closestMeshVertList = new List<int>(3);
            while (queue.Count() > 0)
            {
                // Pull a node off of the queue
                Node curNode = queue.Dequeue();
                // Get the list of nodes adjacent to it
                List<Node> adjacentNodes = adjacencyList.adjacencyList[curNode.index];
                for (int i = 0; i < adjacentNodes.Count; i++)
                { // For each adjacent node, test the distance to that node and queue the node if necessary
                  // Current distance to this node = distance from origin to node + distance between current node and this node
                    float curDist = minDistances[curNode.index] + adjacentNodes[i].weight;
                    if ((curDist < minDistances[adjacentNodes[i].index]) && (curDist < distanceThreshold))
                    { // If we have found a new shorter path that is under the max threshold, save the new distance and enqueue this node
                        minDistances[adjacentNodes[i].index] = curDist;
                        queue.Enqueue(new Node(minDistances[adjacentNodes[i].index], adjacentNodes[i].index));
                    }
                }
                if (curNode.index < uniqueVertices.uniqueMeshVertLength)
                { // If our current node is a mesh vert, add it to the list of closest vertices
                    closestMeshVertList.Add(curNode.index);
                }
            }
            return closestMeshVertList.Distinct().ToArray();
        }
    }
}
