using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
namespace C2M2
{
    using Utilities;
    [Obsolete("Replaced by DijkstraSearch, which does not rely on ObjectManager and simply adds the parts it needs in its constructor")]
    public class DijkstraFindPath
    {
        private ObjectManager objectManager;
        private MeshInfo meshInfo;

        public float[] minDistances { get; private set; }
        public int[] closestMeshVertArr { get; private set; }
        private int lastVertCount = 10;

        #region Classes

        #endregion
        public DijkstraFindPath(ObjectManager objectManager)
        {
            this.objectManager = objectManager;
            meshInfo = objectManager.meshInfo;
            // Save enough space for minDistances
            minDistances = new float[meshInfo.uniqueVerts.Length];
        }
        /// <summary> Finds the shortest path to any node in the graph starting from initialVertIndex below threshold </summary>
        public void FindPath(RaycastHit hit, float distanceThreshold)
        {
            List<Node> initialNodes = meshInfo.BuildInitialNodes(hit);
            // Initialize min distance array to be all Infinity
            minDistances.FillArray(float.PositiveInfinity);
            PriorityQueue queue = new PriorityQueue();
            // Our raycast hits in between three nodes, so we find the distance between our raycast hit point and those nodes, and then queue all of them as our "initial" nodes
            for (int i = 0; i < initialNodes.Count; i++)
            { // Queue each initial node and store its initial weight in the min_distances array
                queue.Enqueue(initialNodes[i]);
                minDistances[initialNodes[i].index] = initialNodes[i].weight;
            }
            // Store a list of the nearest mesh vertex neighbors to our point
            // TODO: How can we guess the size of this list beforehand so that we can preallocate memory for it?
            List<int> closestMeshVertList = new List<int>(lastVertCount);
            while (queue.Count() > 0)
            { // While there are still nodes to check
              // Pull a node off of the queue and get the list of nodes adjacent to it
                Node curNode = queue.Dequeue();
                List<Node> adjacentNodes = meshInfo.adjacencyList[curNode.index];
                for (int i = 0; i < adjacentNodes.Count; i++)
                { // For each adjacent node, test the distance to that node and queue the node if necessary
                  // The current distance to this adjacent node is the shortest distance from the origin to the current node + the distance between the current node and this node
                    float curDist = minDistances[curNode.index] + adjacentNodes[i].weight;
                    if ((curDist < minDistances[adjacentNodes[i].index]) && (curDist < distanceThreshold))
                    { // If we have found a new shorter path that is under the max threshold, save the new distance and enqueue this node
                        minDistances[adjacentNodes[i].index] = curDist;
                        queue.Enqueue(new Node(minDistances[adjacentNodes[i].index], adjacentNodes[i].index));
                    }
                }
                if (curNode.index < meshInfo.uniqueMeshVertLength)
                { // If our current node is a mesh vert, add it to the list of closest vertices
                    closestMeshVertList.Add(curNode.index);
                }
            }
            lastVertCount = closestMeshVertList.Count;
            closestMeshVertArr = closestMeshVertList.Distinct().ToArray();
        }
    }
}
