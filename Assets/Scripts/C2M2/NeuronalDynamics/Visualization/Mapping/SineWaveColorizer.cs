#region using
using System;
using UnityEngine;
using System.Collections.Generic;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    /// SineWaveColorizer
    /// <summary>
    /// A simple sine wave colorizer
    /// The component colorizes a linear geometry or a cylinder surface by a sine wave pattern
    /// Frequency and amplitude can be specified, for instance varying by 10 Hz between colors red and blue
    /// </summary>
    public class SineWaveColorizer : MonoBehaviour
    {
        // User attributes
        [Header("Cylinder Type")]
        public CylinderType cylinder;
        [Header("Wave setup")]
        public WaveType wave;
        public ColorType color;
        private readonly List<Wave> functions = new List<Wave>();
        [Header("Transparency")]
        public float alpha;

        /// Axis
        /// <summary>
        /// Represents one of the coordinate axes
        /// </summary>
        private enum Axis : byte
        {
            x = 0,
            y = 1,
            z = 2,
        }

        /// CylinderType
        /// <summary>
        /// Test cylinder geometries
        /// </summary>
        public enum CylinderType : byte
        {
            One = 1,
            Two = 10,
            Three = 25,
            Five = 50,
            Six = 75,
            Seven = 100
        };

        /// WaveType
        /// <summary>
        /// Specify a wave type
        /// </summary>
        public enum WaveType : byte
        {
            SimpleSine
        }

        /// ColorType
        /// <summary>
        /// Specify a color type
        /// </summary>
        public enum ColorType : byte
        {
            Red,
            Green,
            Blue
        }

        /// <summary>
        ///  Wave delegate
        //tex: $$ y(t) = A\sin(2 \pi f t + \varphi) = A\sin(\omega t + \varphi) $$
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public delegate float Wave(in float t);

        /// <summary>
        ///  SimpleWave
        /// </summary>
        //tex: $$ \text{Simple sine wave with default frequency of: } 2 \pi$$
        /// <param name="t"></param>
        /// <returns></returns>
        public float SimpleWave(in float t)
        {
            return Mathf.Sin(t);
        }

        /// Awake
        /// <summary>
        /// </summary>
        private void Awake()
        {
            functions.Add(SimpleWave);
        }

        /// Start
        /// <summary>
        /// </summary>
        void Start()
        {
            /// Read grid
            UGX.Grid grid = new UGX.Grid(new Mesh(), $"Cylinder {(int)cylinder}");
            UGXReader.ReadUGX(@"C:/Users/tug41634/Desktop/C2M2/c2m2-vr-grids/Cylinders/Cylinder_" + (int)cylinder + ".ugx", ref grid);

            /// Add visual components
            GameObject go = new GameObject(grid.Mesh.name);
            go.AddComponent<MeshFilter>();
            go.GetComponent<MeshFilter>().sharedMesh = grid.Mesh;
            go.AddComponent<MeshRenderer>();
            go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Particles/Standard Surface"));

            /// Colorize with SineWave
            Colorize(grid.Mesh, "z", functions[(int)wave]);
        }

        /// Colorize
        /// <summary>
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="axisName"></param>
        /// <param name="f"></param>
        /// <param name="phi"></param>
        /// <param name="A"></param>
        private void Colorize(Mesh mesh, in string axisName, in Wave wave)
        {
            Vector3 bounds = mesh.bounds.size;
            byte axis = (byte)Enum.Parse(typeof(Axis), axisName);
            float lengthAxis = bounds[axis];
            Vector3[] vertices = mesh.vertices;
            Color32[] colors = new Color32[mesh.vertices.Length];
            int size = vertices.Length;
            for (int i = 0; i < size; i++)
            {
                //tex: Normalize to $$[0, \pi], \textit{where} \quad wave(0) = 0, wave(\pi) = 0$$
                float length = (vertices[i][axis] / lengthAxis) * Mathf.PI;

                // assign color depending on wave height
                colors[i] = GetColor(wave(length));
            }
            mesh.colors32 = colors;
        }

        /// GetColor
        /// <summary>
        /// Get a color depending on the wave height
        /// Color will be red between min and max intensity
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private Color GetColor(in float val)
        {
            switch (color)
            {
                case ColorType.Red: return new Color(val, 0.0f, 0.0f, 0);
                case ColorType.Green: return new Color(0.0f, val, 0.0f, 0);
                case ColorType.Blue: return new Color(0.0f, 0.0f, val, 0);
            }

            return Color.magenta;
        }
    }
}
