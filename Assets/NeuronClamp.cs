using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Utils.MeshUtils;

namespace C2M2.NeuronalDynamics.Interaction
{
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

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;

        public LayerMask layerMask;
        public NeuronSimulation1D activeTarget = null;

        private Tuple<int, double>[] newValues = null;

        private Vector3 lastLocalPos;
        private bool[] posSettled;
        private int framesToSette = 10;

        private Vector3 simLastPos;
        private bool ClampMoved { get { return !lastLocalPos.Equals(transform.localPosition); } }
        private bool ClampSettled {
            get
            {
                for(int i = 0; i < posSettled.Length; i++)
                {
                    if (!posSettled[i]) return false;
                }
                return true;
            }
        }
        private float radius = -1;

        private OVRGrabbable grabbable;
        private MeshRenderer mr;
        private Bounds bounds;

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            bounds = mr.bounds;
            grabbable = GetComponent<OVRGrabbable>();

            if (layerMask.Equals(default(LayerMask)))
            {
                layerMask = LayerMask.GetMask(new string[] { "Raycast" });
            }

            UpdateRadius();

            posSettled = new bool[framesToSette];
        }

        private void FixedUpdate()
        {
            if (ClampMoved || grabbable.isGrabbed)
            {
                // If the clamp is moving, it shouldnt look for a target or have valid points/values
                ClearTarget();
                DeactivateClamp();
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
                        DeactivateClamp();
                        return;
                    }
                }

                // If the clamp is stationary, and we have a valid target, the clamp is hot
                if (clampLive)
                {
                    if(newValues == null || prevPower != clampPower)
                    {
                        newValues = GetNewVals(GetHitPoints());

                        if (prevPower != clampPower) prevPower = clampPower;
                    }

                    if (use1DVerts) activeTarget.Set1DValues(newValues);
                    else activeTarget.SetValues(newValues);
                   
                }
            }
            
            lastLocalPos = transform.localPosition;           
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
            transform.SetParent(activeTarget.transform, true);

            // Translate last location to local space
            lastLocalPos = transform.InverseTransformPoint(lastLocalPos);

            Debug.Log("Clamp childed under " + activeTarget.name + "\nlocalPos: " + transform.localPosition.ToString("F5") + "\nlastLocalPos: " + lastLocalPos.ToString("F5"));
            UpdateRadius();
            return true;
        }
        private void ClearTarget()
        {
            if (activeTarget != null)
            {
                // Transform local position to world space
                lastLocalPos = transform.TransformPoint(lastLocalPos);

                activeTarget = null;
                newValues = null;

                // Free clamp from following simulation
                transform.SetParent(null, true);

                Debug.Log("Clamped unchilded.\nlocalPos: " + transform.localPosition.ToString("F5") + "\nlastLocalPos: " + lastLocalPos.ToString("F5"));
            }
        }
        private List<int> GetHitPoints()
        {
            List<int> hitPoints = new List<int>();

            if (activeTarget == null) return hitPoints;

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

        private float MaxX { get { return transform.localPosition.x + radius; } }
        private float MaxY { get { return transform.localPosition.y + radius; } }
        private float MaxZ { get { return transform.localPosition.z + radius; } }
        private float MinX { get { return transform.localPosition.x - radius; } }
        private float MinY { get { return transform.localPosition.y - radius; } }
        private float MinZ { get { return transform.localPosition.z - radius; } }
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