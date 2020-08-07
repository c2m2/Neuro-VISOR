using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Utils.MeshUtils;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClamp : MonoBehaviour
    {
        [Tooltip("Trigger for clamp, won't send values unless set to true")]
        public bool clampLive = false;
        [Range(0, 1)]
        public double clampPower = 0.1;

        public bool use1DVerts = true;

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;

        public LayerMask layerMask;
        public NeuronSimulation1D activeTarget = null;


        private Tuple<int, double>[] newValues = null;

        private Vector3 lastLocalPos;

        private Vector3 simLastPos;
        private bool ClampMoved { get { return !lastLocalPos.Equals(transform.localPosition); } }

        private OVRGrabbable grabbable;
        private MeshRenderer mr;
        private Bounds bounds;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }
        private BoxHandleResizer handles;
        private Transform handlesParent;

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
        }

        private void FixedUpdate()
        {
            if(activeTarget != null)
            {
                if (clampLive)
                {
                    activeTarget.Set1DValues(newValues);
                }
            }
        }

        public NeuronSimulation1D ReportSimulation(NeuronSimulation1D simulation, Vector3 contactPoint)
        {
            if(activeTarget == null)
            {
                activeTarget = simulation;
                transform.parent = simulation.transform;
                int ind = GetNearestPoint(activeTarget, contactPoint);
                Tuple<int, double> newVal = new Tuple<int, double>(ind, clampPower);
                newValues = new Tuple<int, double>[] { newVal };

            }

            return activeTarget;
        }
        public void ReportExit(NeuronSimulation1D simulation)
        {
            if(activeTarget == simulation)
            {
                activeTarget = null;
            }
            Destroy(this);
        }

        private int GetNearestPoint(NeuronSimulation1D simulation, Vector3 worldPoint)
        {
            // Translate contact point to local space
            Vector3 localPoint = activeTarget.transform.InverseTransformPoint(worldPoint);

            Vector3[] verts;
            if (use1DVerts)
            {
                verts = simulation.Verts1D;
            }
            else {
                MeshFilter mf = simulation.GetComponent<MeshFilter>();
                if (mf == null) return -1;
                Mesh mesh = mf.sharedMesh ?? mf.mesh;
                if (mesh == null) return -1;

                verts = mesh.vertices;
            }

            int nearestVertInd = -1;
            Vector3 nearestPos = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            float nearestDist = float.PositiveInfinity;
            for(int i = 0; i < verts.Length; i++)
            {
                float dist = Vector3.Distance(localPoint, verts[i]);
                if(dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVertInd = i;
                    nearestPos = verts[i];                   
                }
            }

            Debug.Log("Nearest index: " + nearestVertInd 
                + "\nPosition: " + nearestPos.ToString("F5")
                + "\nDistance: " + nearestDist
                + "\nlocalPoint: " + localPoint.ToString("F5")
                + "\nworldPoint: " + worldPoint.ToString("F5")
                + "\nverts.Length: " + verts.Length);

            transform.localPosition = nearestPos;
            return nearestVertInd;
        }

        /*
        private List<int> GetHitPoints()
        {
            List<int> hitPoints = new List<int>();

            if (activeTarget == null) return hitPoints;

            MeshFilter mf = activeTarget.GetComponent<MeshFilter>();
            if (mf == null) return hitPoints;

            Mesh mesh = mf.sharedMesh ?? mf.mesh;
            if (mesh == null) return hitPoints;

            UpdateMaxMin();

            Vector3[] verts = use1DVerts ? activeTarget.Verts1D : mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                if (ClampContains(verts[i]))
                {
                    hitPoints.Add(i);
                }
            }

            return hitPoints;
        }

        private Tuple<int, double>[] GetNewVals(List<int> hitPoints)
        {
            Tuple<int, double>[] newVals = new Tuple<int, double>[hitPoints.Count];
            for (int i = 0; i < newVals.Length; i++)
            {
                newVals[i] = new Tuple<int, double>(hitPoints[i], clampPower);
            }
            return newVals;
        }

        private Vector3 maxLocalPos;
        private Vector3 minLocalPos;
        private bool ClampContains(in Vector3 point)
        {
            //activeTarget.transform.TransformPoint(point);
            //Debug.Log("transform.localPosition: " + transform.localPosition.ToString("F5"));
            //Debug.Log("point: " + point);
            //Debug.Log("radius: " + radius);
            if (point.x > minLocalPos.x && point.y > minLocalPos.y && point.z > minLocalPos.z
                && point.x < maxLocalPos.x && point.y < maxLocalPos.y && point.z < maxLocalPos.z)
                return true;
            else return false;
        }
        
        private void UpdateMaxMin()
        {
            maxLocalPos = transform.localPosition + LocalExtents;
            minLocalPos = transform.localPosition - LocalExtents;
        }
                */

        public void ActivateClamp()
        {
            clampLive = true;
            if (activeMaterial != null)
            {
                mr.material = activeMaterial;
            }
          
            //mr.material.color = activeCol;
        }
        public void DeactivateClamp()
        {
            clampLive = false;
            if (inactiveMaterial != null)
            {
                mr.material = inactiveMaterial;
            }
            //mr.material.color = inactiveCol;
        }

        public void ToggleClamp()
        {
            if (clampLive) DeactivateClamp();
            else ActivateClamp();
        }
    }
}