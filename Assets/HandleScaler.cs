using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Given other handles to follow, maintains proper handle scaling between them
    /// </summary>
    public class HandleScaler : MonoBehaviour
    {
        public enum Axis { x, y, z }
        public Transform x1 = null;
        public Transform x2 = null;
        public Axis xAxis = Axis.x;
        public Transform y1 = null;
        public Transform y2 = null;
        public Axis yAxis = Axis.y;

        private Vector3 origScale;
        private float origXdist;
        private float origYdist;
        private float curXdist;
        private float curYdist;
        private float prevXdist;
        private float prevYdist;
        private bool Xchanged { get { return curXdist == prevXdist; } }
        private bool Ychanged { get { return curYdist == prevYdist; } }
        private float Xdist { get { return Vector3.Distance(x1.position, x2.position); } }
        private float Ydist { get { return Vector3.Distance(y1.position, y2.position); } }
        private float Xscale { get { return Xdist / origXdist; } }
        private float Yscale { get { return Ydist / origYdist; } }

        private Vector3 NewScale
        {
            get
            {
                float xScaler = 1, yScaler = 1, zScaler = 1;

                switch (xAxis)
                {
                    case (Axis.x):
                        xScaler = Xscale;
                        break;
                    case (Axis.y):
                        yScaler = Xscale;
                        break;
                    case (Axis.z):
                        zScaler = Xscale;
                        break;
                }
                switch (yAxis)
                {
                    case (Axis.x):
                        xScaler = Yscale;
                        break;
                    case (Axis.y):
                        yScaler = Yscale;
                        break;
                    case (Axis.z):
                        zScaler = Yscale;
                        break;
                }
                return new Vector3(origScale.x * xScaler, origScale.y * yScaler, origScale.z * zScaler);
            }
        }

        private void Awake()
        {
            if (x1 == null || x2 == null || y1 == null || y2 == null)
            {
                Debug.LogError("Not enough adjacent handles on " + name);
                Destroy(this);
            }
            origScale = transform.localScale;
            origXdist = Xdist;
            origYdist = Ydist;
            prevXdist = origXdist;
            prevYdist = origYdist;
        }

        private void Update()
        {
            curXdist = Xdist;
            curYdist = Ydist;

            if (Xchanged || Ychanged)
            {
                transform.localScale = NewScale;
            }

            prevXdist = curXdist;
            prevYdist = curYdist;
        }
    }
}