using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEngine;

namespace C2M2.NeuronalDynamics.UGX
{
    using DiameterAttachment = IAttachment<DiameterData>;
    /// <summary>
    /// This is the NeuronCell Class, it allows the user to initialize the different quantities of the geometry
    /// and gives some ease of access to the the components of the geometry
    /// </summary>
    public class NeuronCell
    {
        /// <summary>
        /// This is the list of nodes and each node is information attached to it
        /// </summary>
        public List<NodeData> nodeData = new List<NodeData>();
        /// <summary>
        /// this is the edge list, the first integer is the from index and the second index is the to index
        /// </summary>
        public List<Tuple<int, int>> edges = new List<Tuple<int, int>>();
        /// <summary>
        /// this is a list of all the edgelengths, there is a built in routine that computes the edge length
        /// </summary>
        public List<double> edgeLengths = new List<double>();
        /// <summary>
        /// this is the a list of the indices that are the ends of the denrites, they only have one neighbor
        /// </summary>
        public List<int> boundaryID = new List<int>();
        /// <summary>
        /// this is the soma index, this is good know, it is a list because in possible cases
        /// a soma maybe a segment of points, not a singular point
        /// </summary>
        public List<int> somaID = new List<int>();
        /// <summary>
        /// this is the vertiex count, the number of vertices that the 1d graph geometry has
        /// </summary>
        public int vertCount = new int();
        /// <summary>
        /// this is the number of edges in the 1d graph geometry
        /// </summary>
        public int edgeCount = new int();
        /// <summary>
        /// This is the NodeData structure, it will group information together corresponding to each node in the 1D graph geometry
        /// the information stored is analogous to what would be found in the 1D .swc file of the neuron geometry
        /// </summary>
        public struct NodeData
        {
            public int id { get; set; }
            public int pid { get; set; }    // is set somewhere ? this is being used by clamps somewhere and I am not sure how to fix this
            public int pidAlternate { get; set; } // need to fix this at some point!
            public int nodeType { get; set; } // node type
            public double nodeRadius { get; set; }
            public double xcoords { get; set; }
            public double ycoords { get; set; }
            public double zcoords { get; set; }
            public List<int> neighborIDs { get; set; }
        }
        /// <summary>
        /// This will initialize the NeuronCell, the grid parameter is read in
        /// the grid parameter contains the geometric information from the .vrn files
        /// </summary>
        /// <param name="grid"></param>
        public NeuronCell(Grid grid)
        {
            NodeData tempNode = new NodeData();
            VertexAttachementAccessor<DiameterData> accessor = new VertexAttachementAccessor<DiameterData>(grid);
            Mesh mesh = grid.Mesh;
            Vector3[] vertices = mesh.vertices;

            int count = 0;
            int edgeCount = grid.Edges.Count;
            foreach (DiameterData diam in accessor)
            {
                /// this gets the diameter, make sure to divide by 2
                tempNode.nodeRadius = diam.Diameter / 2;
                /// this the node id --> these may not be consecutive
                tempNode.id = grid.Vertices[count].Id;

                /// this for loop is for setting the pid, parent id
                for (int i = 0; i < edgeCount; i++)
                {
                    if (tempNode.id == grid.Edges[i].To.Id){tempNode.pidAlternate = grid.Edges[i].From.Id;}
                    /// the zero-th node has parent -1 --> NOTE: is this alway true, need to check this!!
                    if (tempNode.id == 0){tempNode.pidAlternate = -1;}
                }

                /// these are the actual coordinates of the geometry --> NOTE: these are in [um] already!
                tempNode.xcoords = vertices[count].x;
                tempNode.ycoords = vertices[count].y;
                tempNode.zcoords = vertices[count].z;
                /// this initializes an empty list for the neighbor id nodes
                tempNode.neighborIDs = new List<int>();
                /// if a node has only one neighbor then it is a boundary node
                if (grid.Vertices[count].Neighbors.Count == 1)
                {	
                    /// add the boundary node id to the list
                    boundaryID.Add(grid.Vertices[count].Id);                      
                }
                /// this adds the neigbor ids to the list for this node
                for (int i = 0; i < grid.Vertices[count].Neighbors.Count; i++)
                {
                    tempNode.neighborIDs.Add(grid.Vertices[count].Neighbors[i].Id);
                }

                nodeData.Add(tempNode);
                /// this increments the counter for the number of vertices --> is this even used?? may need to remove!
                count = count + 1;
            }
            vertCount = mesh.vertexCount;
            /// this loop collects the edges and gets the lengths of the edges
            for (int i = 0; i < edgeCount; i++)
            {
                edges.Add((Tuple.Create(grid.Edges[i].From.Id, grid.Edges[i].To.Id)));
                edgeLengths.Add(GetEdgeLength(grid.Edges[i].From.Id, grid.Edges[i].To.Id));
            }

            this.edgeCount = edgeCount;
            this.somaID = grid.Subsets["soma"].Indices.ToList();
        }
        /// <summary>
        /// This small routine writes the geometry file that is used to an output file in .swc format
        /// </summary>
        /// <param name="outFile"></param>
        public void writeSWC(string outFile)
        {
            StreamWriter file = File.AppendText(outFile);
            for (int i = 0; i < this.vertCount; i++)
            {
                file.WriteLine(nodeData[i].id.ToString() + " " + 1.ToString() + " " + nodeData[i].xcoords.ToString() + " " + nodeData[i].ycoords.ToString() + " " + nodeData[i].zcoords.ToString() + " " + nodeData[i].nodeRadius.ToString() + " " + nodeData[i].pidAlternate.ToString());
            }

            file.Close();
        }

        static string cellFormatString = 
            "1D Vert count = {0}\n" +
            "Edge count = {1}\n" +
            "Max edge length = {2}\n" +
            "Avg edge length = {3}\n" +
            "Min edge length = {4}\n" + 
            "Soma ID(s) = ({5})\n" + 
            "Boundary ID(s) = ({6})";
        public override string ToString() => String.Format(
            cellFormatString, 
            vertCount,
            edgeCount, 
            edgeLengths.Max(), 
            edgeLengths.Average(), 
            edgeLengths.Min(),
            String.Join(", ", somaID.Select(c => "'" + c + "'")),
            String.Join(", ", boundaryID.Select(c => "'" + c + "'")));
        /// <summary>
        /// this calculates the euclidean distance between node Start and node End
        /// </summary>
        /// <param name="startId"></param> start node index
        /// <param name="endId"></param> end node index
        /// <returns></returns>
        public double GetEdgeLength(int startId, int endId)
        {
            /// get the coordinates of the start node
            double x1 = nodeData[startId].xcoords;
            double y1 = nodeData[startId].ycoords;
            double z1 = nodeData[startId].zcoords;
            /// get the coordinates of the end node
            double x2 = nodeData[endId].xcoords;
            double y2 = nodeData[endId].ycoords;
            double z2 = nodeData[endId].zcoords;
            /// calculate the square of th differences
            double dx2 = (x1 - x2) * (x1 - x2);
            double dy2 = (y1 - y2) * (y1 - y2);
            double dz2 = (z1 - z2) * (z1 - z2);
            ///return the squareroot
            return Math.Sqrt(dx2 + dy2 + dz2);
        }

    }
}
