using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Interaction.Adjacency
{
    /// <summary> A priority queue represented as a binary heap </summary>
    public class PriorityQueue
    {
        /// <summary> Data for the priority queue </summary>
        private List<Node> data;
        /// <summary> Construct a PriorityQueue </summary>
        public PriorityQueue()
        {
            data = new List<Node>();
        }
        /// <summary> Remove an element from the queue </summary>
        public void Remove(Node item)
        {
            // Find the index of the item to remove and the index of the last element in the list
            int itemIndex = data.IndexOf(item);
            int lastIndex = data.Count - 1;
            // Store item temporarily
            Node midElem = data[itemIndex];
            // Overrite the data at that index with the last element in the list and remove the last item in the list
            data[itemIndex] = data[lastIndex];
            data.RemoveAt(lastIndex);
            // deicrement last index since we have removed an item
            --lastIndex;
            int previousIndex = 0;
            while (true)
            {
                // Traverse down along the binary tree
                int currentIndex = previousIndex * 2 + 1;
                if (currentIndex > lastIndex) break;
                int rc = currentIndex + 1;
                if (rc <= lastIndex && data[rc].CompareTo(data[currentIndex]) < 0)
                {
                    currentIndex = rc;
                }
                if (data[previousIndex].CompareTo(data[currentIndex]) <= 0)
                {
                    break;
                }
                Node tmp = data[previousIndex];
                data[previousIndex] = data[currentIndex];
                data[currentIndex] = tmp;
                previousIndex = currentIndex;
            }
        }
        /// <summary> Add an item to the queue </summary>
        public void Enqueue(Node item)
        {
            data.Add(item);
            int currentIndex = data.Count - 1;
            while (currentIndex > 0)
            {
                int parentIndex = (currentIndex - 1) / 2;
                if (data[currentIndex].CompareTo(data[parentIndex]) >= 0) break;
                Node tmp = data[currentIndex];
                data[currentIndex] = data[parentIndex];
                data[parentIndex] = tmp;
                currentIndex = parentIndex;
            }
        }
        /// <summary> Remove the first element from the queue and return the element </summary>
        public Node Dequeue()
        {
            // Save the first item for returning and move the last item up to the top of the queue
            int lastIndex = data.Count - 1;
            Node firstItem = data[0];
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);
            --lastIndex;
            int parentIndex = 0;
            while (true)
            {
                int currentIndex = parentIndex * 2 + 1;
                if (currentIndex > lastIndex) break;
                int rc = currentIndex + 1;
                if (rc <= lastIndex && data[rc].CompareTo(data[currentIndex]) < 0) currentIndex = rc;
                if (data[parentIndex].CompareTo(data[currentIndex]) <= 0) break;
                Node tmp = data[parentIndex];
                data[parentIndex] = data[currentIndex];
                data[currentIndex] = tmp;
                parentIndex = currentIndex;
            }
            return firstItem;
        }
        /// <summary> Return the first (smallest) element stored in the queue </summary>
        public Node Peek()
        {
            Node frontItem = data[0];
            return frontItem;
        }
        /// <summary> Count the number of elements in the queue </summary>
        public int Count()
        {
            return data.Count;
        }
        /// <summary> Represent the queue as a string </summary>
        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < data.Count; ++i)
                s += data[i].ToString() + " ";
            s += "count = " + data.Count;
            return s;
        }
        /// <summary> Check for the consistency of the priority queue </summary>
        public bool IsConsistent()
        {
            if (data.Count == 0) return true;
            int li = data.Count - 1;
            for (int pi = 0; pi < data.Count; ++pi)
            {
                int lci = 2 * pi + 1;
                int rci = 2 * pi + 2;
                if (lci <= li && data[pi].CompareTo(data[lci]) > 0) return false;
                if (rci <= li && data[pi].CompareTo(data[rci]) > 0) return false;
            }
            return true;
        }
    }
}