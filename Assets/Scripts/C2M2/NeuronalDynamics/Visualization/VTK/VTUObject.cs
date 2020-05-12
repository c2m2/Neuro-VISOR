using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace C2M2.Visualization.VTK
{
    using static VTUColor;
    using Utils;

    public class VTUObject
    {
        public Mesh mesh;
        public float[] componentData { get; set; }
        public float localMax { get; set; }
        public float localMin { get; set; }

        public VTUObject(string name, Vector3[] pointData, int[] cellData, float[] componentData)
        {
            // If our mesh requires more than 65,535 vertices, then we need it in a 32-bit format so it can go up to 4,294,967,295 vertices
            mesh = (pointData.Length < UInt16.MaxValue) ? new Mesh() : new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.name = name;
            mesh.vertices = pointData;
            mesh.triangles = cellData;
            this.componentData = componentData;
            localMax = componentData.Max();
            localMin = componentData.Min();
            mesh.RecalculateNormals();
        }

        public VTUObject(string name, Vector3[] pointData, int[] cellData, float[] componentData, Color32[] colors32)
        {
            mesh.name = name;
            mesh.vertices = pointData;
            mesh.triangles = cellData;
            mesh.colors32 = colors32;
            this.componentData = componentData;
        }

        public void FillColors(float max, float min, Gradient gradient)
        {
            mesh.colors32 = GetVTKColors(max, min, componentData, gradient);
        }
    }
}