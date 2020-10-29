using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Visualization;
using Math = C2M2.Utils.Math;
using C2M2.Interaction;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClamp : MonoBehaviour
    {

        public float radiusRatio = 3f;
        public float heightRatio = 3f;

        public bool clampLive { get; private set; } = false;

        public double clampPower = 55;

        public int focusVert { get; private set; } = -1;
        public Vector3 focusPos;

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;

        public NDSimulation simulation = null;

        private bool use1DVerts = true;

        private MeshRenderer mr;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }
        private Vector3 posFocus = Vector3.zero;
        private Color32 clampCol = Color.black;
        public Color32 ClampCol
        {
            get { return clampCol; }
            private set
            {
                clampCol = value;
                mr.material.SetColor("_Color", clampCol);
            }
        }
        private LUTGradient gradientLUT = null;

        float currentVisualizationScale = 1;

        private void VisualInflationChangeHandler(double newInflation)
        {
            UpdateScale((float)newInflation);
        }

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            OVRInput.Update();
        }
        private void FixedUpdate()
        {
            OVRInput.FixedUpdate();

            if(simulation != null)
            {
                if (clampLive)
                {
                    //activeTarget.Set1DValues(newValues);

                    ClampCol = gradientLUT.EvaluateUnscaled((float)clampPower);
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

        /*
        // Scan new targets for ND simulations
        private void OnTriggerEnter(Collider other)
        {
            if (simulation == null)
            {
                NDSimulation simulation = other.GetComponentInParent<NDSimulation>() ?? other.GetComponent<NDSimulation>();
                if (simulation != null)
                {
                    ReportSimulation(simulation, transform.parent.position);
                }
            }
        }
        */
        public NDSimulation ReportSimulation(NDSimulation simulation, RaycastHit hit)
        {
            if (this.simulation == null)
            {
                this.simulation = simulation;

                transform.parent.parent = simulation.transform;

                int clampIndex = GetNearestPoint(this.simulation, hit);

                NeuronCell.NodeData clampCellNodeData = simulation.NeuronCell.nodeData[clampIndex];

                // Check for duplicates
                if (!VertIsAvailable(clampIndex, simulation))
                {
                    Destroy(transform.parent.gameObject);
                }

                focusVert = clampIndex;
                focusPos = simulation.Verts1D[focusVert];
                Debug.Log("Nearest 1D vert:\nind: " + focusVert + "\npos: " + focusPos);

                SetScale(this.simulation, clampCellNodeData);
                SetRotation(this.simulation, clampCellNodeData);

                simulation.OnVisualInflationChange += VisualInflationChangeHandler;

                // Change object layer to Raycast so the clamp does not continue to interact physically with the simulation
                gameObject.layer = LayerMask.NameToLayer("Raycast");
                Destroy(gameObject.GetComponent<Rigidbody>());

                gradientLUT = this.simulation.GetComponent<LUTGradient>();

                this.simulation.clamps.Add(this);

                transform.parent.localPosition = focusPos;
            }

            return this.simulation;
        }
        /*
        public NDSimulation ReportSimulation(NDSimulation simulation, Vector3 contactPoint)
        {
            if (this.simulation == null)
            {
                this.simulation = simulation;

                transform.parent.parent = simulation.transform;

                int clampIndex = GetNearestPoint(this.simulation, contactPoint);

                NeuronCell.NodeData clampCellNodeData = simulation.NeuronCell.nodeData[clampIndex];

                // Check for duplicates
                if(!VertIsAvailable(clampIndex, simulation))
                {
                    Destroy(transform.parent.gameObject);
                }

                focusVert = clampIndex;

                SetScale(this.simulation, clampCellNodeData);
                SetRotation(this.simulation, clampCellNodeData);

                simulation.OnVisualInflationChange += VisualInflationChangeHandler;

                // Change object layer to Raycast so the clamp does not continue to interact physically with the simulation
                gameObject.layer = LayerMask.NameToLayer("Raycast");
                Destroy(gameObject.GetComponent<Rigidbody>());

                gradientLUT = this.simulation.GetComponent<LUTGradient>();

                this.simulation.clampValues.Add(this);
            }

            return this.simulation;
        }
        */

        private void OnDestroy()
        {
            simulation.clamps.Remove(this);
        }

        private int GetNearestPoint(NDSimulation simulation, RaycastHit hit)
        {
            // Translate contact point to local space

            MeshFilter mf = simulation.transform.GetComponentInParent<MeshFilter>();
            if (mf == null) return -1;
            // Get mesh vertices from hit triangle
            int triInd = hit.triangleIndex * 3;
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Find nearest 1D vert to these 3D verts
            int[] verts1D = new int[]{
                simulation.Map[v1].lambda < 0.5 ? simulation.Map[v1].v1 : simulation.Map[v1].v2,
                simulation.Map[v2].lambda < 0.5 ? simulation.Map[v2].v1 : simulation.Map[v2].v2,
                simulation.Map[v3].lambda < 0.5 ? simulation.Map[v3].v1 : simulation.Map[v3].v2};

            float nearestDist = float.PositiveInfinity;
            int nearestVert1D = -1;
            foreach(int vert in verts1D)
            {
                float dist = Vector3.Distance(hit.point, simulation.Verts1D[vert]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVert1D = vert;
                }
            }

            return nearestVert1D;
        }
            /*
            private int GetNearestPoint(NDSimulation simulation, Vector3 worldPoint)
            {
                // Translate contact point to local space
                Vector3 localPoint = this.simulation.transform.InverseTransformPoint(worldPoint);

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

                posFocus = nearestPos;

                transform.parent.localPosition = posFocus;

                transform.parent.name = "AttachedNeuronClamp" + nearestVertInd;
                return nearestVertInd;
            }
            */

        // Returns true if the that 1D index is available, otherwise returns false
        private bool VertIsAvailable(int clampIndex, NDSimulation simulation)
        {
            bool validLocation = true;
            float distanceBetweenClamps = simulation.AverageDendriteRadius * heightRatio * 2;

            foreach (NeuronClamp clamp in simulation.clamps)
            {
                // If there is a clamp on that 1D vertex, the spot is not open
                if (clamp.focusVert == clampIndex)
                {
                    Debug.LogWarning("Clamp already exists on focus vert [" + clampIndex + "]");
                    validLocation = false;
                }
                // If there is a clamp within distance of 2, the spot is not open
                else if ((clamp.transform.parent.localPosition - transform.parent.localPosition).magnitude < distanceBetweenClamps)
                {
                    Debug.LogWarning("Clamp too close to clamp located on focus vert [" + clamp.focusVert + "].");
                    validLocation = false;
                }
            }
            return validLocation;
        }

        private void SetScale(NDSimulation simulation, NeuronCell.NodeData cellNodeData)
        {
            currentVisualizationScale = (float)simulation.VisualInflation;

            float radiusScalingValue = radiusRatio * (float)cellNodeData.nodeRadius;
            float heightScalingValue = heightRatio * simulation.AverageDendriteRadius;

            //Ensures clamp is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            transform.parent.localScale = new Vector3(radiusLength, radiusLength, heightScalingValue);
        }

        public void UpdateScale(float newScale)
        {
            if (this != null)
            {
                float modifiedScale = newScale/currentVisualizationScale;
                Vector3 tempVector = transform.parent.localScale;
                tempVector.x *= modifiedScale;
                tempVector.y *= modifiedScale;
                transform.parent.localScale = tempVector;
                currentVisualizationScale = newScale;
            }

        }

        public void SetRotation(NDSimulation simulation, NeuronCell.NodeData cellNodeData)
        {
            List<int> neighbors = cellNodeData.neighborIDs;

            // Get each neighbor's Vector3 value
            List<Vector3> neighborVectors = new List<Vector3>();
            foreach (int neighbor in neighbors)
            {
                neighborVectors.Add(simulation.Verts1D[neighbor]);
            }

            Vector3 rotationVector;
            if (neighborVectors.Count == 2)
            {
                Vector3 clampToFirstNeighborVector = neighborVectors[0] - simulation.Verts1D[cellNodeData.id];
                Vector3 clampToSecondNeighborVector = neighborVectors[1] - simulation.Verts1D[cellNodeData.id];

                rotationVector = clampToFirstNeighborVector.normalized - clampToSecondNeighborVector.normalized;
            }
            else if (neighborVectors.Count > 0)
            {
                rotationVector = simulation.Verts1D[cellNodeData.pid].normalized - transform.parent.localPosition.normalized;
            }
            else
            {
                rotationVector = Vector3.up; //if a clamp has no neighbors it will use a default orientation of facing up
            }
            transform.parent.localRotation = Quaternion.LookRotation(rotationVector);
        }

        #region Input
        [Tooltip("Hold down a raycast for this many frames in order to destroy a clamp")]
        public int destroyCount = 50;
        int holdCount = 0;
        float thumbstickScaler = 1;

        /// <summary>
        /// Pressing this button toggles clamps on/off. Holding this button down for long enough destroys the clamp
        /// </summary>
        public OVRInput.Button toggleDestroyOVR = OVRInput.Button.Two;
        public OVRInput.Button toggleDestroyOVRS = OVRInput.Button.Four;
        private bool PressedToggleDestroy
        {
            get
            {
                if (GameManager.instance.vrIsActive)
                    return (OVRInput.Get(toggleDestroyOVR) || OVRInput.Get(toggleDestroyOVRS));
                else return true;
            }
        }
        public KeyCode powerModifierPlusKey = KeyCode.UpArrow;
        public KeyCode powerModifierMinusKey = KeyCode.DownArrow;
        private float PowerModifier
        {
            get
            {
                if (GameManager.instance.vrIsActive)
                {
                    // Use the value of whichever joystick is held up furthest
                    float y1 = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                    float y2 = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                    float scaler = (y1 + y2);

                    return thumbstickScaler * scaler;
                }
                else
                {
                    if (Input.GetKey(powerModifierPlusKey)) return thumbstickScaler;
                    if (Input.GetKey(powerModifierMinusKey)) return -thumbstickScaler;
                    else return 0;
                }
            }
        }
        private bool powerClick = false;
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            if (PressedToggleDestroy)
                holdCount++;
            else
                CheckInput();

            float power = PowerModifier;
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !powerClick) powerClick = true;

            clampPower += power;
        }

        public void ResetInput()
        {
            CheckInput();
        }

        private void CheckInput()
        {
            if (!powerClick)
            {
                if (holdCount >= destroyCount)
                {
                    Destroy(transform.parent.gameObject);
                }
                else if (holdCount > 0) ToggleClamp();
            }

            holdCount = 0;
            powerClick = false;
        }

        #endregion
    }
}
