using UnityEngine;
using C2M2.Visualization;

namespace C2M2.Simulation
{
    using Utils;
    using Interaction.VR;
    /// <summary>
    /// Simulation of type double[] for visualizing scalar fields on mesh surfaces
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class SurfaceSimulation : Simulation<double[], Mesh>
    {
        #region Variables

        /// <summary>
        /// Gradient for coloring each surface point based on their scalar values
        /// </summary>
        public Gradient gradient;

        /// <summary>
        /// Lookup table for more efficient color calculations on the gradient
        /// </summary>
        public LUTGradient colorLUT { get; private set; } = null;

        public LUTGradient.ExtremaMethod extremaMethod = LUTGradient.ExtremaMethod.RollingExtrema;
        [Tooltip("Must be set if extremaMethod is set to GlobalExtrema")]
        public float globalMax = float.NegativeInfinity;
        [Tooltip("Must be set if extremaMethod is set to GlobalExtrema")]
        public float globalMin = float.PositiveInfinity;

        protected Mesh colliderMesh = null;

        private MeshFilter mf;
        private MeshRenderer mr;
        #endregion

        /// <summary>
        /// Update vertex colors based on simulation values
        /// </summary>
        private void UpdateVisualization(in float[] scalars3D)
        {
            Color32[] newCols = colorLUT.Evaluate(scalars3D);
            if(newCols != null)
            {
                mf.mesh.colors32 = newCols;
            }
        }
        protected override void UpdateVisualization(in double[] newValues) => UpdateVisualization(newValues.ToFloat());

        #region Unity Methods
        protected sealed override void OnAwake(Mesh viz)
        {
            if (!dryRun)
            {
                // Safe check for existing MeshFilter, MeshRenderer
                mf = GetComponent<MeshFilter>();
                if(mf == null) mf = gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = viz;

                mr = GetComponent<MeshRenderer>();
                if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

                // Ensure the renderer has a vertex coloring material     
                mr.material = GameManager.instance.vertexColorationMaterial;

                InitColors();
                
                VRRaycastableMesh raycastable = gameObject.AddComponent<VRRaycastableMesh>();

                Debug.Log(colliderMesh.name);
                if (colliderMesh != null) raycastable.SetSource(colliderMesh);
                else raycastable.SetSource(viz);

                // Add custom grabbable here

            }
            return;

            void InitColors()
            {
                // Initialize the color lookup table
                colorLUT = gameObject.AddComponent<LUTGradient>();
                colorLUT.Gradient = gradient;
                colorLUT.extremaMethod = extremaMethod;
                if (extremaMethod == LUTGradient.ExtremaMethod.GlobalExtrema)
                {
                    colorLUT.globalMax = globalMax;
                    colorLUT.globalMin = globalMin;
                }
            }
        }
        #endregion
    }
}