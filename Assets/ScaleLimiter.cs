using UnityEngine;
using C2M2.Utils.MeshUtils;

namespace C2M2.Interaction {
    public class ScaleLimiter : MonoBehaviour
    {
        [Tooltip("Source of the mesh to monitor for size")]
        public MeshFilter meshFilter = null;
        [Tooltip("If set to 0, GameManager.maxObjectScale will be used")]
        public Vector3 maxScale = Vector3.zero;
        [Tooltip("If set to 0, GameManager.minObjectScale will be used")]
        public Vector3 minScale = Vector3.zero;

        // Current size indicators
        private float SizeX { get { return meshFilter.sharedMesh.bounds.size.x * meshFilter.transform.localScale.x; } }
        private float SizeY { get { return meshFilter.sharedMesh.bounds.size.y * meshFilter.transform.localScale.y; } }
        private float SizeZ { get { return meshFilter.sharedMesh.bounds.size.z * meshFilter.transform.localScale.z; } }

        // Maximum and minimum indicators
        private float MaxX { get { return maxScale.x; } }
        private float MaxY { get { return maxScale.y; } }
        private float MaxZ { get { return maxScale.z; } }
        private float MinX { get { return minScale.x; } }
        private float MinY { get { return minScale.y; } }
        private float MinZ { get { return minScale.z; } }

        // Shortcuts to test if mesh is inappropriately sized
        private bool tooSmall
        {
            get { return (SizeX < MinX) || (SizeY < MinY) || (SizeZ < MinZ); }
        }
        private bool tooBig
        {
            get { return (SizeX > MaxX) || (SizeY > MaxY) || (SizeZ > MaxZ); }
        }

        private Vector3 prevScale = Vector3.zero;
        bool prevScaleFound = false;

        private void Awake()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError("No MeshFilter given to ScaleLimiter on " + name);
                    Destroy(this);
                }
            }

            if (maxScale.Equals(Vector3.zero))
                maxScale = GameManager.instance.objMaxScale;

            if (minScale.Equals(Vector3.zero))
                minScale = GameManager.instance.objMinScale;

        }
        // Update is called once per frame
        private void Update()
        {
            if (meshFilter.sharedMesh == null) return;

            CheckScale();
        }

        private void CheckScale()
        {
            // If we have a previously valid scale, we just revert to that since it's faster than rescaling manually
            if (tooSmall)
            {
                if (prevScaleFound) meshFilter.transform.localScale = prevScale;
                else meshFilter.sharedMesh.Rescale(transform, minScale);
                return;
            }
            if (tooBig)
            {
                if (prevScaleFound) meshFilter.transform.localScale = prevScale;
                else meshFilter.sharedMesh.Rescale(transform, maxScale);
                return;
            }

            prevScale = meshFilter.transform.localScale;
            prevScaleFound = true;
        }
    }
}