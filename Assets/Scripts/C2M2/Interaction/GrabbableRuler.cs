using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Utils.MeshUtils;

namespace C2M2.Interaction {
    public class GrabbableRuler : MonoBehaviour
    {
        public Transform scaleTarget = null;
        public Transform handleA = null;
        public Transform handleB = null;
        [Tooltip("If true and scaleTarget has a mesh attached, rescales scaleTarget to GameManager.instance.objScaleDefault")]
        public bool initSize = true;

        public Color rulerCol = Color.black;
        public float rulerWidth = 0.05f;

        private LineRenderer lineRend = null;
        private Vector3[] HandlePositions { get { return new Vector3[] { handleA.transform.position, handleB.transform.position }; } }

        private Vector3 MaxSize { get { return GameManager.instance.objScaleMax; } }
        private Vector3 MinSize { get { return GameManager.instance.objScaleMin; } }
        // Divide the current handle distance by the original and multiply it by the original scale
        private Vector3 NewScale { get { return Scaler * origScale; } }
        private Vector3 origScale;
        private float Scaler { get { return CurDist / origDist; } }
        private float origDist = -1f;
        private float CurDist { get { return Vector3.Distance(handleA.transform.position, handleB.transform.position); } }
        private float minX = 0.05f;
        private float maxX = 0.95f;

        private void Awake()
        {
            if (scaleTarget == null || handleA == null || handleB == null)
                Destroy(this);
            
            // Assumea object is at GameManager.instance.objDefaultScale
            handleB.localPosition = new Vector3(((maxX - minX) / 2), handleB.localPosition.y, handleB.localPosition.z);
            Debug.Log("handleB position: " + handleB.position.ToString("F5"));
            origScale = scaleTarget.localScale;
            origDist = CurDist;

            Debug.Log("origScale: " + origScale.ToString("F5") + "\norigDist: " + origDist);

            InitLineRend();
        }

        private void Start()
        {
            if (initSize)
            {
                MeshFilter mf = scaleTarget.GetComponent<MeshFilter>();
                if (mf == null) return;
                Mesh mesh = mf.sharedMesh;
                if(mesh == null)
                {
                    mesh = mf.mesh;
                    if (mesh == null) return;
                }

                Vector3 midSize = (MaxSize - MinSize) / 2;
                mesh.Rescale(scaleTarget, midSize);
                origScale = scaleTarget.localScale;
            }
        }

        // maxX = maxScale
        // minX = minScale

        private void Update()
        {
            LimitHandlePos();

            Debug.Log("NewScale: " + NewScale.ToString("F5"));
            scaleTarget.localScale = NewScale;

            lineRend.SetPositions(HandlePositions);
        }

        private void InitLineRend()
        {
            lineRend = gameObject.AddComponent<LineRenderer>();

            lineRend.material = GameManager.instance.lineRendMaterial;

            lineRend.startWidth = rulerWidth;
            lineRend.endWidth = rulerWidth;

            lineRend.startColor = rulerCol;
            lineRend.endColor = rulerCol;

            lineRend.positionCount = 2;
            lineRend.SetPositions(HandlePositions);
        }

        private void LimitHandlePos()
        {
            if(handleB.localPosition.x <= minX)
            {
                handleB.localPosition = new Vector3(minX, handleB.localPosition.y, handleB.localPosition.z);
            }else if(handleB.localPosition.x >= maxX)
            {
                handleB.localPosition = new Vector3(maxX, handleB.localPosition.y, handleB.localPosition.z);
            }
            Debug.Log("handleB position: " + handleB.position.ToString("F5"));
        }
    }
}