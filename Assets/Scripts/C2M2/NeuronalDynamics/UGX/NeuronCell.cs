using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace C2M2.NeuronalDynamics.UGX
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
            public int pid { get; set; }    // pid not use
            public int nodeType { get; set; } // node type
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
                tempNode.id = grid.Vertices[count].Id;

                tempNode.xcoords = grid.Mesh.vertices[count].x;
                tempNode.ycoords = grid.Mesh.vertices[count].y;
                tempNode.zcoords = grid.Mesh.vertices[count].z;

                tempNode.neighborIDs = new List<int>();

                if (grid.Vertices[count].Neighbors.Count == 1)
                {
			
                    boundaryID.Add(grid.Vertices[count].Id);
                      
                }

                for (int i = 0; i < grid.Vertices[count].Neighbors.Count; i++)
                {
                    tempNode.neighborIDs.Add(grid.Vertices[count].Neighbors[i].Id);
                }

                nodeData.Add(tempNode);
                count = count + 1;
            }
            vertCount = grid.Mesh.vertexCount;

            for (int i = 0; i < grid.Edges.Count(); i++)
            {
                edges.Add((Tuple.Create(grid.Edges[i].From.Id, grid.Edges[i].To.Id)));
                edgeLengths.Add(GetEdgeLength(grid.Edges[i].From.Id, grid.Edges[i].To.Id));
            }

            this.edgeCount = this.edges.Count();
            this.somaID = grid.Subsets["soma"].Indices.ToList();
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

        public double GetEdgeLength(int startId, int endId)
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
