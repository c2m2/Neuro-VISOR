using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
namespace C2M2
{
    /// <summary>
    /// Read in a map associating 1D neuron vertices to 3D mesh vertices.
    /// </summary>
    public class Neuron1D : MonoBehaviour
    {
        /// WORKFLOW:
        ///     Read in 1D->3D mapping
        ///     Run some color function on the 1D vertices
        ///     Translate colors to the 3D vertices
        [Tooltip("OBJ File info")]
        public string objFilePath = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar + "tuf91497" + Path.DirectorySeparatorChar + "Desktop" + Path.DirectorySeparatorChar + "C2M2" + Path.DirectorySeparatorChar + "Y_shape" + Path.DirectorySeparatorChar + "Y_shape.obj";
        [Tooltip("Directory path of data")]
        public string mapPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "1DNeuron" + Path.DirectorySeparatorChar + "Y_shape_1D3D.map";
        public Gradient gradient;
        public Neuron1DVertMap vertMap { get; private set; }
        private MeshFilter meshFilter3D;
        private void Awake()
        {
            //Debug.Log("original path: " + AssetDatabase.GetAssetPath(gameObject.transform.parent.gameObject));
            meshFilter3D = GetComponent<MeshFilter>();
            if (meshFilter3D == null) { gameObject.AddComponent<MeshFilter>(); }
            // Build and rescale our obj mesh
            Mesh newMesh = OBJFileReader.ReadFile(objFilePath);
            newMesh.Rescale(transform);
            meshFilter3D.mesh = newMesh;
            vertMap = Neuron1DMapFileReader.ReadMapFile(mapPath);
        }
        public void Start()
        {
            //StartCoroutine(TestSimulation(0.02f));
        }
        /// <summary>
        /// Add some dummy initial conditions onto the 1D neurons and watch the initial values diffuse over the 3D surface
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        private IEnumerator TestSimulation(float waitTime)
        {
            Color32[] colors32 = meshFilter3D.mesh.colors32;
            // Create an array of scalar values and fill the first 1/3 of indices with value 1
            float[] scalars1D = new float[vertMap.count1D];
            for (int i = 0; i < scalars1D.Length / 3; i++) { scalars1D[i] = 1f; }
            float diffusionConstant = 0.1f;
            //float initialValue = ArrayHelpers.Sum(scalars1D);
            while (true)
            {
                float[] deltas = new float[scalars1D.Length];   // Store changes before applying them to maintain integrity of diffusion
                float diffusionAmount;
                // First index only has one neighbor
                diffusionAmount = 2 * diffusionConstant * scalars1D[0];
                deltas[1] += diffusionAmount;
                deltas[0] -= diffusionAmount;
                for (int i = 1; i < scalars1D.Length - 1; i++)
                {
                    diffusionAmount = diffusionConstant * scalars1D[i];
                    // Diffuse to previous vert
                    deltas[i - 1] += diffusionAmount;
                    deltas[i] -= diffusionAmount;
                    // Diffuse to next vert
                    deltas[i + 1] += diffusionAmount;
                    deltas[i] -= diffusionAmount;
                }
                // Last index only has one neighbor
                diffusionAmount = 2 * diffusionConstant * scalars1D[scalars1D.Length - 1];
                deltas[scalars1D.Length - 2] += diffusionAmount;
                deltas[scalars1D.Length - 1] -= diffusionAmount;
                // Apply deltas to scalars array
                for (int i = 0; i < scalars1D.Length; i++) { scalars1D[i] += deltas[i]; }
                // Apply colors based on new scalar values
                EvaluateColors32(colors32, in scalars1D);
                yield return new WaitForSecondsRealtime(waitTime);
            }
        }
        private void EvaluateColors32(Color32[] colors32, in float[] scalars1D)
        {
            if (colors32 == null || colors32.Length == 0) { colors32 = new Color32[vertMap.count3D]; }
            for (int i = 0; i < vertMap.count1D; i++)
            { // for each 1D point,
                Color32 curCol = gradient.Evaluate(scalars1D[i]);
                // Get the list of associated 3D points,
                List<int> verts3D = vertMap.Get3DVerts(vertMap.verts1D[i]);
                for (int j = 0; j < verts3D.Count; j++)
                {
                    colors32[verts3D[j]] = curCol;
                }
            }
            meshFilter3D.mesh.colors32 = colors32;
        }
    }
}
