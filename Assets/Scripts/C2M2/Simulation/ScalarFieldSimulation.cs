using UnityEngine;
using C2M2.Visualization;

namespace C2M2.Simulation
{
    using Utils;
    /// <summary>
    /// Simulation of type double[] for visualizing scalar fields on meshes
    /// </summary>
    public abstract class ScalarFieldSimulation : Simulation<double[]>
    {
        #region Variables

        public Gradient gradient;
        public LUTGradient.ExtremaMethod extremaMethod = LUTGradient.ExtremaMethod.RollingExtrema;
        public float globalMax;
        public float globalMin;

        public LUTGradient colorLUT { get; private set; } = null;
        private MeshFilter mf;
        private MeshRenderer mr;
        #endregion

        #region Abstract Methods
        /// <summary> Retrieve simulation values as a double array </summary>
        /// <remarks> The simulation must make an array of scalars available in order for visaulization to work.
        /// The array should contain one scalar value for every point that needs to be visualized </remarks>
        //public override abstract double[] GetValues();
        protected abstract Mesh BuildMesh();
        #endregion

        protected override void UpdateVisualization(in double[] newValues) => UpdateVisualization(newValues.ToFloat());
        // Update the scalar field on the mesh
        private void UpdateVisualization(in float[] scalars3D)
        {
            Color32[] newCols = colorLUT.Evaluate(scalars3D);
            if(newCols != null)
            {
                mf.mesh.colors32 = newCols;
            }
        }

        #region Unity Methods
        protected sealed override void OnAwake()
        {
            // Safe check for existing MeshFilter, MeshRenderer
            mf = GetComponent<MeshFilter>();
            if(mf == null)
                mf = gameObject.AddComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            if (mr == null)
                mr = gameObject.AddComponent<MeshRenderer>();
            mr.material = GameManager.instance.vertexColorationMaterial;

            // Scalar field simulations need to color said field onto the object surface
            colorLUT = gameObject.AddComponent<LUTGradient>();
            colorLUT.Gradient = gradient;
            colorLUT.extremaMethod = extremaMethod;
            if(extremaMethod == LUTGradient.ExtremaMethod.GlobalExtrema)
            {
                colorLUT.globalMax = globalMax;
                colorLUT.globalMin = globalMin;
            }

            // Create mesh for visualization
            Mesh mesh = BuildMesh();
            mf.sharedMesh = mesh;
        }
        #endregion
    }
}
