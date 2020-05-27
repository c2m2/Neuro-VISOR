using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Instantiate the Raycastee prefab and create a mesh collider on it
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class RaycastMeshCollider : MonoBehaviour
    {
        private GameObject raycasteePrefab;
        public void Build(GameObject gameObject)
        { // Instantiate raycastee prefab, add mesh collider & set its shared mesh to be the current MeshFilter mesh
            Instantiate((GameObject)Resources.Load("Prefabs/Raycastee"), gameObject.transform).AddComponent<MeshCollider>().sharedMesh = gameObject.GetComponent<MeshFilter>().mesh;
            Destroy(this);
        }
    }
}