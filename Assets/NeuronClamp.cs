﻿using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Utils.MeshUtils;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClamp : MonoBehaviour
    {
        public bool clampLive { get; private set; } = false;
        [Range(0, 1)]
        public double clampPower = 0.1;

        public bool use1DVerts = true;

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;

        public NeuronSimulation1D activeTarget = null;
        public Mesh cylMesh;
        private Mesh origMesh;


        private Tuple<int, double>[] newValues = null;

        private Vector3 lastLocalPos;
        private Vector3 origScale;

        private Vector3 simLastPos;
        private bool ClampMoved { get { return !lastLocalPos.Equals(transform.localPosition); } }

        private OVRGrabbable grabbable;
        private MeshRenderer mr;
        private Bounds bounds;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            origScale = transform.localScale;
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

        // Scan new targets for ND simulations
        private void OnTriggerEnter(Collider other)
        {
            if (activeTarget == null)
            {
                NeuronSimulation1D simulation = other.GetComponentInParent<NeuronSimulation1D>() ?? other.GetComponent<NeuronSimulation1D>();
                if (simulation != null)
                {
                    ReportSimulation(simulation, transform.position);
                    //GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }

        public NeuronSimulation1D ReportSimulation(NeuronSimulation1D simulation, Vector3 contactPoint)
        {
            if (activeTarget == null)
            {
                activeTarget = simulation;
                transform.parent = simulation.transform;


                int ind = GetNearestPoint(activeTarget, contactPoint);
                Tuple<int, double> newVal = new Tuple<int, double>(ind, clampPower);
                newValues = new Tuple<int, double>[] { newVal };

                //origMesh = GetComponent<MeshFilter>().sharedMesh;
                //GetComponent<MeshFilter>().sharedMesh = cylMesh;

                // Set scale here
                UpdateScale(activeTarget, ind);
            }

            return activeTarget;
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
            else
            {
                MeshFilter mf = simulation.GetComponent<MeshFilter>();
                if (mf == null) return -1;
                Mesh mesh = mf.sharedMesh ?? mf.mesh;
                if (mesh == null) return -1;

                verts = mesh.vertices;
            }

            int nearestVertInd = -1;
            Vector3 nearestPos = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            float nearestDist = float.PositiveInfinity;
            for (int i = 0; i < verts.Length; i++)
            {
                float dist = Vector3.Distance(localPoint, verts[i]);
                if (dist < nearestDist)
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

            this.name = "AttachedNeuronClamp" + nearestVertInd;
            return nearestVertInd;
        }

        private void UpdateScale(NeuronSimulation1D simulation, int nearestPoint)
        {
            Vector3[] verts = simulation.Verts1D;

            // Approach 1: Resize relative to geometry bounds
            /*
            float scaler = 1 / 45;
            // If the geometry is 90 units long, make the clamp 2 units in radius
            float minX = -1, minY = -1, minZ = -1;
            float maxX = -1, maxY = -1, maxZ = -1;
            Vector3 localExtents = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            transform.localScale = localExtents * scaler;
            */


            // Approach 2: Get position of neighboring 1D points, fit between neighbors
            /*
            Vector3 ourPos = verts[nearestPoint]; // = however we get this info
            Vector3 nPosA = new Vector3(); 
            Vector3 nPosB = new Vector3();

            float halfwayA = Vector3.Distance(ourPos, nPosA);
            float halfwayB = Vector3.Distance(ourPos, nPosB);
            float avg = (halfwayA + halfwayB) / 2;

            transform.localScale = new Vector3(avg, avg, avg);
            */

        }

        private void OnTriggerExit(Collider other)
        {
            if (activeTarget != null)
            {
                if (activeTarget == other.GetComponentInParent<NeuronSimulation1D>() || activeTarget == other.GetComponent<NeuronSimulation1D>())
                {
                    activeTarget = null;
                    // Only a clamp instantiator should be allowed to remove a NeuronClamp from a simulation
                    if (transform.parent == null || transform.parent.GetComponent<NeuronClampAnchor>() == null)
                        Destroy(this);

                    transform.localScale = origScale;
                }
            }
        }
    }
}