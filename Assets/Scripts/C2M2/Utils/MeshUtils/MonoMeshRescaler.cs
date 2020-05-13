using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace C2M2 {
    namespace Utils
    {
        namespace MeshUtils
        {
            /// <summary>
            /// Monobehaviour to allow rescaling meshes from the editor using static Utilities.MeshUtilities
            /// </summary>
            [ExecuteInEditMode]
            public class MonoMeshRescaler : MonoBehaviour
            {
                public Vector3 targetSize = Vector3.one;
                [Tooltip("If true, mesh vertices will be rescaled individually. Otherwise transform will be scaled")]
                public bool rescaleMesh = false;
                public void Rescale()
                {
                    MeshFilter mf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
                    Mesh mesh = GetComponent<MeshFilter>().sharedMesh ?? throw new MeshNotFoundException();
                    mesh.Rescale(transform, targetSize);
                    mf.sharedMesh = mesh;
                    DestroyImmediate(this);
                }
            }
#if UNITY_EDITOR
            [CustomEditor(typeof(MonoMeshRescaler))]
            public class RescaleMeshEditor : UnityEditor.Editor
            {
                public override void OnInspectorGUI()
                {
                    DrawDefaultInspector();
                    var script = (MonoMeshRescaler)target;
                    if (GUILayout.Button("Rescale")) script.Rescale();
                }
            }
#endif
        }
    }
}
