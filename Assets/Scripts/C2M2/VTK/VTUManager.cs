using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    using Interaction;
    
    public class VTUManager : MonoBehaviour
    {
        private ObjectManager objectManager;
        [Header("Data Information")]
        [Tooltip("Type of file to read in")]
        public string dataExtension = "*.vtu";
        [Tooltip("Directory path of data")]
        public string dataPath = "data";
        [Tooltip("Render the first X files in the directory, instead of the entire directory. If negative, the script renders all files")]
        public int fileProcessCount = -1;
        [Header("Transform")]
        public Vector3 initialPosition = Vector3.zero;
        public Vector3 initialRotation = Vector3.zero;
        [Header("Interaction Features")]
        public int compoundColliderResolution = 75;
        public List<VTUObject> vtuList { get; private set; }
        [Tooltip("How many invisible edges to add to each existing edge for the purpose of distance measurement along the mesh surface")]
        [Range(0, 9)]
        public int adjacencyListSubdivisions = 1;
        [Header("Menu")]
        public VTUPlayer VTUPlayer;
        private VTUObjectBuilder vtuObjectBuilder = new VTUObjectBuilder();
        private MeshFilter meshf;
        public Gradient gradient { get; set; }

        public void Initialize(ObjectManager objectManager)
        {
            this.objectManager = objectManager;
            meshf = GetComponent<MeshFilter>();
            // Read files and build VTU objects
            vtuList = BuildVTUList();
            if (meshf != null)
            {
                meshf.sharedMesh = vtuList[0].mesh;
            }
            else
            {
                Debug.LogError("No MeshFilter found on " + name);
            }
            // Build compound collider & send result to OVRGrabbable
            GetComponent<Interaction.VR.PublicOVRGrabbable>().M_GrabPoints = 
                NonConvexMeshCollider.Calculate(gameObject, compoundColliderResolution); ;
            // Build raycastee mesh collider
            RaycastMeshCollider buildRaycastMeshCollider = gameObject.AddComponent<RaycastMeshCollider>();
            buildRaycastMeshCollider.Build(gameObject);
            // Rescale & position object
            vtuList[0].mesh.Rescale(transform);
            transform.position = initialPosition;
            transform.eulerAngles = initialRotation;
            // 6. Add DijkstraObject and initialize
            VTUPlayer.Initialize();
        }
        private List<VTUObject> BuildVTUList()
        {
            dataPath = Application.streamingAssetsPath + System.IO.Path.DirectorySeparatorChar + dataPath + System.IO.Path.DirectorySeparatorChar;
            if (fileProcessCount < 0)
            {
                return vtuObjectBuilder.BuildVTUObjects(dataPath, dataExtension, gradient);
            }
            else
            {
                return vtuObjectBuilder.BuildVTUObjects(dataPath, dataExtension, gradient, fileProcessCount);
            }
        }
    }
}