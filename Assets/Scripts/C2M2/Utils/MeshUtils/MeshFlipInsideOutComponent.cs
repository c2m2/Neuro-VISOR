using UnityEngine;
using System;

namespace C2M2
{
    namespace Utils
    {
        using MeshUtils;
        [ExecuteInEditMode]
        public class MeshFlipInsideOutComponent : MonoBehaviour
        {
            private void Update()
            {
                FlipInsideOut();
            }
            public void FlipInsideOut()
            {
                try
                {
                    MeshFilter mf = GetComponent<MeshFilter>() ?? throw new MeshFilterNotFoundException();
                    Mesh mesh = mf.sharedMesh ?? throw new MeshNotFoundException();
                    mesh.ReverseTriangles();
                    mf.sharedMesh = mesh;
                }catch(Exception e)
                {
                    Debug.LogError(e);
                }
                DestroyImmediate(this);
            }
        }
    }
}
