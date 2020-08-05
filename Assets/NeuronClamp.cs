using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Utils.MeshUtils;

namespace C2M2.NeuronalDynamics.Interaction {

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(OVRGrabbable))]
    public class NeuronClamp : MonoBehaviour
    {
        [Tooltip("Trigger for clamp, won't send values unless set to true")]
        public bool clampLive = false;
        [Range(0, 1)]
        public double clampPower = 0.1;
        private double prevPower = 0.1;

        public bool use1DVerts = true;

        public LayerMask layerMask;
        public NeuronSimulation1D activeTarget = null;

        private List<int> targetPoints;
        private Tuple<int, double>[] newValues;

        private Vector3 thisLastPos;
        private Vector3 simLastPos;
        private bool ClampMoved { get { return !thisLastPos.Equals(transform.position); } }
        private bool NeuronMoved { get { return !simLastPos.Equals(activeTarget.transform.position); } }
        private float radius = -1;
        private float MaxX { get { return transform.localPosition.x + radius; } }
        private float MaxY { get { return transform.localPosition.y + radius; } }
        private float MaxZ { get { return transform.localPosition.z + radius; } }
        private float MinX { get { return transform.localPosition.x - radius; } }
        private float MinY { get { return transform.localPosition.y - radius; } }
        private float MinZ { get { return transform.localPosition.z - radius; } }

        private MeshFilter mf;
        private OVRGrabbable grabbable;
        private Bounds bounds;



        private void Awake()
        {
            if (layerMask.Equals(default(LayerMask)))
            {
                layerMask = LayerMask.GetMask(new string[] { "Raycast" });
            }

            bounds = GetComponent<MeshRenderer>().bounds;
            mf = GetComponent<MeshFilter>();
            grabbable = GetComponent<OVRGrabbable>();

            UpdateRadius();
        }

        private void Update()
        {
            if (ClampMoved || grabbable.isGrabbed)
            {
                // If the clamp is moving, it shouldnt look for a target or have valid points/values
                ClearTarget();
                clampLive = false;
            }
            else
            {
                // If our clamp stops moving, look for a simulation to target
                if (activeTarget == null)
                {
                    LookForNewTarget();
                    // If we find a valid target, get the points to update values for
                    if(activeTarget == null)
                    { 
                        // If we didn't find a target, clamp should be parented by nothing 
                        ClearTarget();
                        clampLive = false;
                        return;
                    }
                }

                // If the clamp is stationary, and we have a valid target, the clamp is hot
                if (clampLive)
                {
                    if(newValues == null || prevPower != clampPower)
                    {
                        targetPoints = GetHitPoints();
                        newValues = GetNewVals(targetPoints);

                        if (prevPower != clampPower) prevPower = clampPower;
                    }

                    if (use1DVerts) activeTarget.Set1DValues(newValues);
                    else activeTarget.SetValues(newValues);
                   
                }
            }
            if(transform.parent == null)
            {
                Debug.Log("world position: " + transform.position.ToString("F5") + "\nlocalScale: " + transform.localScale + "\nparent: null");
            }
            else
            {
                Debug.Log("world position: " + transform.position.ToString("F5") + "\nlocalScale: " + transform.localScale + "\nparent: " + transform.parent.name);
            }
            
            thisLastPos = transform.position;
        }

        private bool LookForNewTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, layerMask.value);
            if(hits.Length == 0)
            {
                activeTarget = null;
                return false;
            }
            foreach (Collider hit in hits)
            {
                activeTarget = hit.GetComponent<NeuronSimulation1D>() ?? hit.GetComponentInParent<NeuronSimulation1D>();
            }
            if (activeTarget == null) return false;

            // Set simulation to be parent of clamp so that it follows
            //Vector3 posTemp = transform.position;
           // transform.parent = activeTarget.transform;
            transform.SetParent(activeTarget.transform, true);
            //transform.position = posTemp;

            UpdateRadius();
            return true;
        }
        private void ClearTarget()
        {
            if (activeTarget != null)
            {
                activeTarget = null;

                // Set null parent since clamp is no longer following parent
               // Vector3 posTemp = transform.position;
                transform.SetParent(null, true);
                //transform.position = posTemp;
            }
        }
        private List<int> GetHitPoints()
        {
            List<int> hitPoints = new List<int>();
            MeshFilter mf = activeTarget.GetComponent<MeshFilter>();
            if (mf == null) return hitPoints;

            Mesh mesh = mf.sharedMesh ?? mf.mesh;
            if (mesh == null) return hitPoints;

            UpdateRadius();

            Vector3[] verts = use1DVerts ? activeTarget.Verts1D : mesh.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                if (ClampContains(verts[i]))
                {
                    hitPoints.Add(i);
                }
            }
            //ClampContains(verts[0]);

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

        private bool ClampContains(in Vector3 point)
        {
            //activeTarget.transform.TransformPoint(point);
            //Debug.Log("transform.localPosition: " + transform.localPosition.ToString("F5"));
            //Debug.Log("point: " + point);
            //Debug.Log("radius: " + radius);
            if (point.x > MinX && point.x < MaxX
                && point.y > MinY && point.y < MaxY
                && point.z > MinZ && point.z < MaxZ) return true;
            else return false;
        }

        private void UpdateRadius()
        {
            radius = transform.localScale.x / 2;
        }
    }
}