using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Utils.MeshUtils;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using C2M2.NeuronalDynamics.UGX;

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

        private Tuple<int, double>[] newValues = null;

        private Vector3 lastLocalPos;
        private Vector3 origScale;

        private Vector3 simLastPos;
        private bool ClampMoved { get { return !lastLocalPos.Equals(transform.localPosition); } }

        private OVRGrabbable grabbable;
        private MeshRenderer mr;
        private Bounds bounds;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }
        private Vector3 posFocus = Vector3.zero;

        float currentVisualizationScale = 1;

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            origScale = transform.parent.localScale;
            grabbable = GetComponentInParent<OVRGrabbable>();
        }

        private void Update()
        {
            if (activeTarget != null)
            {
                transform.parent.localPosition = posFocus;
            }
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
        }
        public void DeactivateClamp()
        {
            clampLive = false;
            if (inactiveMaterial != null)
            {
                mr.material = inactiveMaterial;
            }
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
                    ReportSimulation(simulation, transform.parent.position);
                    //GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }

        public NeuronSimulation1D ReportSimulation(NeuronSimulation1D simulation, Vector3 contactPoint)
        {
            if (activeTarget == null)
            {
                activeTarget = simulation;
                //Vector3 pos = transform.parent.position;
                transform.parent.parent = simulation.transform;
                //transform.parent.position = pos;


                int ind = GetNearestPoint(activeTarget, contactPoint);
                Tuple<int, double> newVal = new Tuple<int, double>(ind, clampPower);
                newValues = new Tuple<int, double>[] { newVal };

                //origMesh = GetComponent<MeshFilter>().sharedMesh;
                //GetComponent<MeshFilter>().sharedMesh = cylMesh;

                // Set scale here
                SetScale(activeTarget, ind);
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

            posFocus = nearestPos;

            transform.parent.name = "AttachedNeuronClamp" + nearestVertInd;
            return nearestVertInd;
        }

        [Tooltip("Hold down a raycast for this many frames in order to destroy a clamp")]
        public int destroyCount = 50;
        int holdCount = 0;
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void ListenForDestroy()
        {
            holdCount++;
            if(holdCount == destroyCount)
            {
                Debug.Log("Destroying " + transform.parent.name);
                Destroy(transform.parent.gameObject);
            }
        }
        public void ResetHoldCount()
        {
            holdCount = 0;
        }

        private void SetScale(NeuronSimulation1D simulation, int nearestPoint)
        {
            //Gets the neighbors of the vertex
            //List<UGX.Vertex> neighbors = simulation.getGrid1D().Vertices[nearestPoint].Neighbors;

            //Gets the visual inflation
            currentVisualizationScale = (float) simulation.VisualInflation;

            float scalarSize = 20f; //Needs to be tested to find right number
                                   //Could also adjusted so that it can be edited in Unity Editor

            NeuronCell testCell = new NeuronCell(simulation.getGrid1D());
            double dendriteWidth = testCell.nodeData[nearestPoint].nodeRadius;
            //can also get neighbors for nodeData
            Debug.Log("Nearest Point Radius: " + dendriteWidth);

            float scalingVal = (float)(scalarSize * dendriteWidth * currentVisualizationScale);
            transform.parent.localScale = new Vector3(scalingVal, scalingVal, scalingVal);
        }

        public void UpdateScale(float newScale)
        {
            float modifiedScale = newScale/currentVisualizationScale;
            transform.parent.localScale *= modifiedScale;
            currentVisualizationScale = newScale;
        }

        /*
        private void OnTriggerExit(Collider other)
        {
            if (activeTarget != null)
            {
                if (activeTarget == other.GetComponentInParent<NeuronSimulation1D>() || activeTarget == other.GetComponent<NeuronSimulation1D>())
                {
                    activeTarget = null;

                    transform.parent.localScale = origScale;
                    posFocus = Vector3.zero;
                }
            }
        }*/
    }
}