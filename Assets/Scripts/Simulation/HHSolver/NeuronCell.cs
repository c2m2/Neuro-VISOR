using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;
using System.IO;
using UnityEngine;
namespace C2M2
{
    namespace UGX
    {
        using DiameterAttachment = IAttachment<DiameterData>;
	
        
        public class NeuronCell
        {
            public List<NodeData> nodeData = new List<NodeData>();
            public List<Tuple<int, int>> edges = new List<Tuple<int, int>>();
            public List<double> edgeLengths = new List<double>();
            public List<int> boundaryID = new List<int>();
            public List<int> somaID = new List<int>();
            
            public int vertCount = new int();
            public int edgeCount = new int();

            public struct NodeData
            {
                public int id { get; set; }
                public int pid { get; set; }
                public int nodeType { get; set; }
                public double nodeRadius { get; set; }
                public double xcoords { get; set; }
                public double ycoords { get; set; }
                public double zcoords { get; set; }
                public List<int> neighborIDs { get; set; }
            }

            public NeuronCell(Grid grid)
            {

                AttachmentHandler.Available();
                NodeData tempNode = new NodeData();
                VertexAttachementAccessor<DiameterData> accessor = new VertexAttachementAccessor<DiameterData>(grid);

                int count = 0;
                foreach (DiameterData diam in accessor)
                {
                    tempNode.nodeRadius = diam.Diameter / 2;
                    tempNode.id = Algebra.GetDoFIndex(grid.Vertices[count].Id, ordering);

                    tempNode.xcoords = grid.Mesh.vertices[count].x;
                    tempNode.ycoords = grid.Mesh.vertices[count].y;
                    tempNode.zcoords = grid.Mesh.vertices[count].z;

                    tempNode.neighborIDs = new List<int>();

                    if (grid.Vertices[count].Neighbors.Count == 1)
                    {
			
                        boundaryID.Add(Algebra.GetDoFIndex(grid.Vertices[count].Id, ordering));
                      
                    }

                    for (int i = 0; i < grid.Vertices[count].Neighbors.Count; i++)
                    {
                        tempNode.neighborIDs.Add(Algebra.GetDoFIndex(grid.Vertices[count].Neighbors[i].Id, ordering));
                    }

                    nodeData.Add(tempNode);
                    count = count + 1;
                }
                vertCount = grid.Mesh.vertexCount;

                for (int i = 0; i < grid.Edges.Count(); i++)
                {
                    edges.Add((Tuple.Create(Algebra.GetDoFIndex(grid.Edges[i].From.Id, ordering), Algebra.GetDoFIndex(grid.Edges[i].To.Id, ordering))));
                    edgeLengths.Add(GetEdgeLength(Algebra.GetDoFIndex(grid.Edges[i].From.Id, ordering), Algebra.GetDoFIndex(grid.Edges[i].To.Id, ordering)));
                }
                this.edgeCount = this.edges.Count();

                this.somaID = grid.Subsets["soma"].Indices.ToList();
            }

            // For reading in an swc file
            public void ReadDataTable(string filePath)
            {
                NodeData dr = new NodeData();

                string[] lines = System.IO.File.ReadAllLines(filePath);
                int lineCount = 0;

                foreach (string line in lines)
                {
                    var cols = line.Split(' ');

                    dr.id = int.Parse(cols[0], System.Globalization.NumberStyles.Integer);
                    dr.nodeType = int.Parse(cols[1], System.Globalization.NumberStyles.Integer);

                    dr.xcoords = double.Parse(cols[2], System.Globalization.NumberStyles.Any);
                    dr.ycoords = double.Parse(cols[3], System.Globalization.NumberStyles.Any);
                    dr.zcoords = double.Parse(cols[4], System.Globalization.NumberStyles.Any);

                    dr.nodeRadius = double.Parse(cols[5], System.Globalization.NumberStyles.Any);
                    dr.pid = int.Parse(cols[6], System.Globalization.NumberStyles.Integer);

                    if (lineCount > 0)
                    {
                        edges.Add((Tuple.Create(dr.id, dr.pid)));
                    }

                    nodeData.Add(dr);

                    lineCount = lineCount + 1;
                }

                vertCount = lineCount;
                edgeCount = vertCount - 1;
            }

            static string nodeFormatString =
                "ID = {0}\n" +
                "Coordinates =  ({1}, {2}, {3})\n" +
                "{4}";
            public string NodeToString(int nodeId) => String.Format(nodeFormatString, nodeData[nodeId].id, nodeData[nodeId].xcoords, nodeData[nodeId].ycoords, nodeData[nodeId].zcoords, NodeNeighborsToString(nodeId));
            private string NodeNeighborsToString(int nodeId)
            {
                string s = "Node " + nodeId + ": " + nodeData[nodeId].neighborIDs.Count + " Neighbors:";
                for (int i = 0; i < nodeData[nodeId].neighborIDs.Count; i++)
                {
                    s += "\n\t" + nodeData[nodeId].neighborIDs[i];
                }
                return s;
            }

            static string edgeFormatString =
                "Start ID = {0}\n" +
                "End ID = {1}\n" +
                "Length = {2}";
            public void EdgeToString(int edgeId) => String.Format(edgeFormatString, edges[edgeId].Item1, edges[edgeId].Item2, GetEdgeLength(edges[edgeId].Item1, edges[edgeId].Item2));

            static string cellFormatString = 
                "Vert count = {0}\n" +
                "Edge count = {1}\n" +
                "Max edge length = {2}\n" +
                "Avg edge length = {3}\n" +
                "Min edge length = {4}";
            public override string ToString() => String.Format(cellFormatString, vertCount, edgeCount, edgeLengths.Max(), edgeLengths.Average(), edgeLengths.Min());

            private double GetEdgeLength(int startId, int endId)
            {
                double x1 = nodeData[startId].xcoords;
                double y1 = nodeData[startId].ycoords;
                double z1 = nodeData[startId].zcoords;

                double x2 = nodeData[endId].xcoords;
                double y2 = nodeData[endId].ycoords;
                double z2 = nodeData[endId].zcoords;

                double dx2 = (x1 - x2) * (x1 - x2);
                double dy2 = (y1 - y2) * (y1 - y2);
                double dz2 = (z1 - z2) * (z1 - z2);

                return Math.Sqrt(dx2 + dy2 + dz2);
            }

        }
    }
}
