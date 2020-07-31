using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction {
    public class GrabbableRuler : MonoBehaviour
    {
        public Transform scaleTarget = null;
        public Transform handleA = null;
        public Transform handleB = null;

        public Color rulerCol = Color.black;
        public float rulerWidth = 0.05f;

        private LineRenderer lineRend = null;
        private Vector3[] HandlePositions { get { return new Vector3[] { handleA.transform.position, handleB.transform.position }; } }

        // Divide the current handle distance by the original and multiply it by the original scale
        private Vector3 NewScale { get { return Scaler * origScale; } }
        private Vector3 origScale;
        private float Scaler { get { return CurDist / origDist; } }
        private float origDist = -1f;
        private float CurDist { get { return Vector3.Distance(handleA.transform.position, handleB.transform.position); } }
        private float minX = 0f;
        private float maxX = 1.5f;

        private void Awake()
        {
            if (scaleTarget == null || handleA == null || handleB == null)
                Destroy(this);

            origScale = scaleTarget.localScale;
            origDist = CurDist;

            InitLineRend();
            maxX = maxX / transform.localScale.x;
        }

        private void Update()
        {
            LimitHandlePos();
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
        }
    }
}