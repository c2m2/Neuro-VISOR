﻿#region using
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using UnityEditor;
using System.Xml.Schema;
using System.Collections.Generic;
using System.IO;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    using DiameterAttachment = IAttachment<DiameterData>;
    using MappingAttachment = IAttachment<MappingData>;
    using NormalAttachment = IAttachment<NormalData>;
    /// <summary>
    /// Simple UGX reader class
    /// </summary>
    public static class UGXReader
    {
        private static readonly byte MAPPING_FIELDS = 7;
        private static readonly string XSD_SCHEMA = @Application.dataPath + Path.DirectorySeparatorChar + "ugx.xsd";
        private static readonly string UGX_EXTENSION = ".ugx";
        public static Boolean Validate = false;

        /// ReadUGX
        /// <summary>
        /// Read an UGX and returns a UGXFile which encapsulates 1) The Unity Mesh and 2) The attachments
        /// </summary>
        public static void ReadUGX()
        {
            Debug.Log(Application.dataPath);
            Grid grid = new Grid(new Mesh());
            grid.Attach(new DiameterAttachment());
            // ReadUGX(@"C:/Users/tug41634/Desktop/cube_3d.ugx", ref grid);
            ReadUGX(@"C:/Users/tug41634/Downloads/mouseSmallCell3.ugx", ref grid);

            foreach (Edge e in grid.Edges)
            {
                Debug.Log("Edge: " + e.From.Id + " ... " + e.To.Id);
            }
        }

        /// ReadUGX
        /// <summary>
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static void ReadUGX(ref Grid grid)
        {
            grid.Attach(new DiameterAttachment());
            ReadUGX(@"C:/Users/tug41634/Desktop/cube_3d.ugx", ref grid);
        }

        /// ReadUGX
        /// <summary>
        /// </summary>
        /// <param name="filename"> name of UGX file on disk </param>
        /// <returns> </returns>
        public static void ReadUGX(in string filename, ref Grid grid)
        {
            if (!UGX_EXTENSION.Equals(Path.GetExtension(filename), StringComparison.InvariantCultureIgnoreCase))
            {
                Debug.LogWarning("Supplied file does not have UGX extension >>{UGX_EXTENSION}<<");
            }

            /// Check if diameter data was atttached
            if (!grid.HasVertexAttachment<DiameterAttachment>())
            {
                grid.Attach(new DiameterAttachment());
            }

            Debug.Log("Reading UGX...");
            Vector3[] vertices;
            int[] triangles;

            // Create the XmlSchemaSet class and add the schema
            XmlSchemaSet sc = new XmlSchemaSet();
            sc.Add("", XSD_SCHEMA);

            // Set the validation settings
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            if (!Validate) settings.ValidationType = ValidationType.None;
            settings.Schemas = sc;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (object sender, ValidationEventArgs e) => Debug.Log($"Validation Error:\n {e.Message}\n");

            /// Parse the UGX file and validate before
            using (XmlReader reader = XmlReader.Create(new StreamReader(filename), settings))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        XElement element = XNode.ReadFrom(reader) as XElement;
                        if (element != null)
                        {
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// VERTICES
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "vertices")
                            {
                                XAttribute name = element.Attribute("coords");
                                int dim = int.Parse(name.Value);
                                if (dim != 3)
                                {
                                    Debug.LogError($"Only dimension 3 supported, but provided was {dim}d.");
                                }

                                float[] indices = Array.ConvertAll(element.Value.Split(' '), float.Parse);
                                int size = indices.Length / 3;
                                vertices = new Vector3[size];
                                for (int i = 0; i < size; i++)
                                {
                                    /// TOOD: Right/left handed coordinate system in UGX format? Matches Unity's?
                                    vertices[i] = new Vector3(indices[i * 3], indices[i * 3 + 1], indices[i * 3 + 2]);
                                    grid.Vertices.Add(new Vertex(i));
                                }
                                grid.Mesh.vertices = vertices;
                            }

                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// EDGES
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "edges")
                            {
                                int[] indices = Array.ConvertAll(element.Value.Split(' '), int.Parse);
                                int size = indices.Length / 2;
                                List<Edge> edges = new List<Edge>(size);
                                for (int i = 0; i < size; i++)
                                {
                                    //edges.Add(new Edge(new Vertex(indices[i * 2]), new Vertex(indices[(i * 2) + 1])));
                                    edges.Add(new Edge(grid.Vertices[indices[(i * 2)]], grid.Vertices[indices[(i * 2) + 1]]));
                                    grid.Vertices[indices[i * 2]].Neighbors.Add(grid.Vertices[indices[(i * 2) + 1]]);
                                    grid.Vertices[indices[(i * 2) + 1]].Neighbors.Add(grid.Vertices[indices[(i * 2)]]);
                                }
                                grid.Edges = edges;
                            }

                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// TRIANGLES
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "triangles")
                            {
                                triangles = Array.ConvertAll(element.Value.Split(' '), int.Parse);
                                grid.Mesh.triangles = triangles;
                            }

                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// TETRAHEDRONS
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "tetrahedrons")
                            {
                                Debug.LogWarning("Geometric element (Tetrahedron) currently not supported.");
                                Debug.LogWarning("Only surface elements directly supported in Unity right now.");
                            }

                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// QUADRILATERAL
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "quadrilateral")
                            {
                                Debug.LogError("Geometric element (Quadrilateral) currently not supported.");
                            }

                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// VERTEX_ATTACHMENT
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "vertex_attachment")
                            {
                                XAttribute name = element.Attribute("name");
                                //////////////////////////////////////////////////////////////////////////////////////////
                                /// DIAMETER
                                //////////////////////////////////////////////////////////////////////////////////////////
                                if (name.Value == "diameter")
                                {
                                    if (grid.HasVertexAttachment<DiameterAttachment>())
                                    {
                                        XAttribute type = element.Attribute("type");
                                        if (type.Value == "double")
                                        {
                                            double[] diameters = Array.ConvertAll(element.Value.Split(' '), double.Parse);
                                            VertexAttachementAccessor<DiameterData> accessor =
                                                new VertexAttachementAccessor<DiameterData>(grid, diameters.Length, new DiameterData());

                                            for (int i = 0; i < diameters.Length; i++)
                                            {
                                                accessor[i] = new DiameterData(diameters[i]);
                                            }

                                        }
                                        else
                                        {
                                            Debug.LogWarning("Diameter vertex attachment is required to have type of double.");
                                        }
                                    }
                                }
                                //////////////////////////////////////////////////////////////////////////////////////////
                                /// NORMALS
                                //////////////////////////////////////////////////////////////////////////////////////////
                                if (name.Value == "npNormals")
                                {
                                    if (grid.HasVertexAttachment<NormalAttachment>())
                                    {
                                        XAttribute type = element.Attribute("type");
                                        if (type.Value == "vector3")
                                        {
                                            float[] normals = Array.ConvertAll(element.Value.Split(' '), float.Parse);
                                            VertexAttachementAccessor<NormalData> accessor =
                                                new VertexAttachementAccessor<NormalData>(grid, normals.Length / 3, new NormalData());

                                            for (int i = 0; i < normals.Length / 3; i++)
                                            {
                                                accessor[i] = new NormalData(new Vector3(normals[i * 3], normals[(i * 3) + 1], normals[(i * 3) + 2]));
                                            }

                                        }
                                        else
                                        {
                                            Debug.LogWarning("Vertex attachment (npNormals) is required to have type of vector3.");
                                        }
                                    }
                                }

                                //////////////////////////////////////////////////////////////////////////////////////////
                                /// SYNAPSES
                                //////////////////////////////////////////////////////////////////////////////////////////
                                if (name.Value == "synapses")
                                {
                                    Debug.LogWarning("Vertex attachment (synapses) currently not supported.");
                                    string[] data = element.Value.Split(' ');

                                    VertexAttachementAccessor<SynapseData> accessor =
                                            new VertexAttachementAccessor<SynapseData>(
                                                grid, 0, new SynapseData());
                                    int index = 0;
                                    int fieldIndex = 0;
                                    while (true)
                                    {
                                        switch (Enum.Parse(typeof(SynapseType),
                                            data[index]))
                                        {
                                            case SynapseType.ALPHA_POST:
                                                accessor[index] = new SynapseData(new AlphaPostSynapse());
                                                fieldIndex += 4;
                                                break;
                                            case SynapseType.EXP2:
                                                accessor[index] = new SynapseData(new EXP2Synapse());
                                                fieldIndex += 7;
                                                break;
                                            case SynapseType.UNDEF:
                                                accessor[index] = new SynapseData(new UndefSynapse());
                                                fieldIndex++;
                                                break;

                                        }
                                    }

                                }

                                //////////////////////////////////////////////////////////////////////////////////////////
                                /// MAPPING
                                //////////////////////////////////////////////////////////////////////////////////////////
                                if (name.Value == "npMapping")
                                {
                                    if (grid.HasVertexAttachment<MappingAttachment>())
                                    {
                                        float[] mappings = Array.ConvertAll(element.Value.Split(' '), float.Parse);
                                        int size = mappings.Length / MAPPING_FIELDS;
                                        VertexAttachementAccessor<MappingData> accessor =
                                             new VertexAttachementAccessor<MappingData>(grid, size, new MappingData());

                                        if (mappings.Length % MAPPING_FIELDS != 0)
                                        {
                                            Debug.LogError("Mapping vertex attachment (npMapping) not in correct format.");
                                        }

                                        for (int i = 0; i < size; i++)
                                        {
                                            accessor[i] = new MappingData(
                                                new Vector3(mappings[i * MAPPING_FIELDS], mappings[(i * MAPPING_FIELDS) + 1],
                                                            mappings[(i * MAPPING_FIELDS) + 2]),
                                                new Vector3(mappings[(i * MAPPING_FIELDS) + 3], mappings[(i * MAPPING_FIELDS) + 4],
                                                            mappings[(i * MAPPING_FIELDS) + 5]),
                                                mappings[(i * MAPPING_FIELDS) + 6]
                                            );
                                        }
                                    }
                                }
                            }

                            //////////////////////////////////////////////////////////////////////////////////////////////
                            /// SUBSETS
                            //////////////////////////////////////////////////////////////////////////////////////////////
                            if (element.Name == "subset_handler")
                            {
                                Func<String, XElement, int[]> GetIndices = (String s, XElement el) =>
                                   Array.ConvertAll(el.Element("vertices").Value.Split(' '), int.Parse);

                                Func<String, XElement, Boolean> HasValue = (String s, XElement el) =>
                               !String.IsNullOrEmpty(el.Attribute(s).Value);

                                if ("defSH".Equals(element.Attribute("name").Value))
                                {
                                    foreach (XElement el in element.Elements().Where(el => HasValue("name", el)))
                                    {
                                        String subsetName = el.Attribute("name").Value;
                                        grid.Subsets[subsetName] = new Subset(subsetName, GetIndices(subsetName, el));
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("No subsets contained in input grid provided to UGXReader.");
                                }
                            }
                        }
                    }
                }
            }

            /// GridUtils.AvailableSubsets(grid);
            Debug.Log("done!");
        }
    }
}
