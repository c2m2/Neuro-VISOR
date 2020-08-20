#region using
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    /// AttachmentInfo
    /// <summary>
    /// Encapsulate attachment information as immutable
    /// </summary>
    /// Attachment data is stored in Data and
    /// Attachment type is stored in Types
    public readonly struct AttachmentInfo
    {
        /// AttachmentInfo
        /// <summary>
        /// Construct an empty AttachmentInfo
        /// </summary>
        /// <param name="attachments"> Name of attachment </param>
        /// <param name="types"> TYpe of attachment </param>
        public AttachmentInfo(Dictionary<string, Attachment> attachments, Dictionary<string, Type> types)
        {
            Types = types;
            Data = attachments;
        }

        public Dictionary<string, Type> Types { get; }
        public Dictionary<string, Attachment> Data { get; }

        /// Add
        /// <summary>
        /// Adds an attachment with name and type to the Types and Data dicts
        /// </summary>
        /// <param name="name"> Attachment's name </param>
        /// <param name="type"> Attachment's type </param>
        /// <param name="attachment"> Attachment itself </param>
        public void Add(in string name, in Type type, in Attachment attachment)
        {
            Types.Add(name, type);
            Data.Add(name, attachment);
        }
    }

    /// Grid
    /// <summary>
    /// Encapsulates Unity mesh and the edges of the UGX file 
    /// </summary>
    public class Grid
    {
        // Available types and data for attachments for this grid
        public Dictionary<string, Type> types = new Dictionary<string, Type>();
        public Dictionary<string, Attachment> data = new Dictionary<string, Attachment>();
        public AttachmentInfo AttachmentInfo { get; }

        // Unity's mesh (Includes the vertices and triangles)
        public Mesh Mesh { get; set; }
        // Edges from UGX (Not needed)
        public List<Edge> Edges { get; set; }
        // Vertices from UGX (Can get neighbors/edges with this list)
        public List<Vertex> Vertices { get; set; }
        /// SubsetInformation
        public SubsetCollection Subsets { get; } = new SubsetCollection();
        /// Grid order type
        internal OrderType Type { get; set; } = OrderType.Identity;

        /// ToString
        /// <summary>
        /// Returns a string representation of the Grid containign useful information
        /// </summary>
        /// <returns> String representation of a Grid</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Grid has name >>{Mesh.name}<< and #{Mesh.vertices.Length}");
            sb.Append($" vertices and #{AttachmentInfo.Data.Count} attachments. ");
            int counter = 1;
            foreach (var pair in AttachmentInfo.Data)
            {
                sb.Append($" Attachment #{counter} ({pair.Key}) has type ");
                sb.Append(AttachmentInfo.Types[pair.Key].FullName);
                counter++;
            }
            sb.Append($" There are #{Edges.Count} edges contained in the grid");
            return sb.ToString();
        }

        /// Grid
        /// <summary>
        /// Constructs an empty grid with a given name
        /// </summary>
        /// <param name="Mesh"> Unity mesh </param>
        /// <param name="name"> Grid's name </param>
        public Grid(in Mesh Mesh, in string name = "UGX Grid")
        {
            Edges = new List<Edge>();
            Vertices = new List<Vertex>();
            this.Mesh = Mesh;
            this.Mesh.name = name;
            AttachmentInfo = new AttachmentInfo(
                new Dictionary<string, Attachment>(),
                new Dictionary<string, Type>()
            );
        }

        /// HasVertexAttachment
        /// <summary>
        /// Check if a grid contains a certain vertex attachmen with type T
        /// </summary>
        /// <typeparam name="T"> Name of the vertex attachment </typeparam>
        /// <returns> True if vertex with given type present otherwise false </returns>
        public bool HasVertexAttachment<T>()
        {
            return AttachmentInfo.Types.ContainsValue(typeof(T));
        }

        /// Clear
        /// <summary>
        /// Clears the attachment with name from the attachment lists for this grid
        /// </summary>
        /// <param name="name"> Name of attachment </param>
        public void Clear(in string name)
        {
            AttachmentInfo.Types.Remove(name);
            AttachmentInfo.Data.Remove(name);
        }
    }

    /// Vertex
    /// <summary>
    /// Immutable vertex representation
    /// </summary>
    readonly public struct Vertex
    {
        public List<Vertex> Neighbors { get; }
        public int Id { get; }
        /// Vertex
        /// <summary>
        /// Construct a vertex
        /// </summary>
        /// <param name="Id"> Vertex index corresponds to Unity's mesh index of vertex for now</param>
        /// <param name="Neighbors"> Direct Neighbor Ids of this vertex </param>
        public Vertex(in int Id)
        {
            this.Id = Id;
            Neighbors = new List<Vertex>();
        }
    }

    /// Edge
    /// <summary>
    /// Immutable edge representation
    /// </summary>
    readonly public struct Edge
    {
        public Vertex From { get; }
        public Vertex To { get; }
        /// Edge
        /// <summary>
        /// Construct an edge from two vertices
        /// </summary>
        /// <param name="from">Vertex</param>
        /// <param name="to">Vertex</param>
        public Edge(in Vertex From, in Vertex To)
        {
            this.From = From;
            this.To = To;
        }
    }
}
