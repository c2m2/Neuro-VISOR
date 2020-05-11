using UnityEngine;

namespace C2M2
{
    using Interaction;
    public class VTUMeshBuilder : MonoBehaviour
    {
        [Header("Data Information")]
        [Tooltip("Type of file to read in")]
        public string dataExtension = "*.vtu";
        [Tooltip("Directory path of data")]
        public string dataPath = "VTUData";
        private VTUObjectBuilder vtuObjectBuilder = new VTUObjectBuilder();
        private void Awake()
        {
            MeshFilter mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
            Gradient32LUT gradLUT = GetComponent<Gradient32LUT>() ?? gameObject.AddComponent<Gradient32LUT>();
            Gradient gradient = gradLUT.Gradient;
            // Find our vtu data in streaming assets
            dataPath = Application.streamingAssetsPath + System.IO.Path.DirectorySeparatorChar + dataPath + System.IO.Path.DirectorySeparatorChar;
            // Only read the first vtu file for simplicity
            VTUObject vtuObject = vtuObjectBuilder.BuildVTUObjects(dataPath, dataExtension, gradient, 1)[0];
            vtuObject.mesh.Rescale(transform);
            mf.sharedMesh = vtuObject.mesh;
        }
    }
}
