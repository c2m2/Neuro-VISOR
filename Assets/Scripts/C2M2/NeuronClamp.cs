using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Visualization;
using Math = C2M2.Utils.Math;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClamp : MonoBehaviour
    {
        public float radiusRatio = 3f;
        public float heightRatio = 1f;
        [Tooltip("The highlight sphere's radius is some real multiple of the clamp's radius")]
        public float highlightSphereScale = 3f;

        public bool clampLive { get; private set; } = false;

        public double clampPower = 55;

        public int focusVert { get; private set; } = -1;
        public Vector3 focusPos;

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;

        public NDSimulation simulation = null;

        private MeshRenderer mr;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }
        private Vector3 posFocus = Vector3.zero;

        private Color32 inactiveCol = Color.black;
        public Color32 InactiveCol
        {
            get
            {
                return inactiveCol;
            }
            set
            {
                inactiveCol = value;
                inactiveMaterial.SetColor("_Color", inactiveCol);
            }
        }
        private Color32 activeCol = Color.white;
        public Color32 ActiveColor
        {
            get { return activeCol; }
            private set
            {
                activeCol = value;
                activeMaterial.SetColor("_Color", activeCol);
            }
        }
        private LUTGradient gradientLUT = null;

        float currentVisualizationScale = 1;
        private void VisualInflationChangeHandler(double newInflation)
        {
            UpdateScale((float)newInflation);
        }

        public GameObject highlightObj;
        public float minHighlightGlobalSize = 0.1f * (1/3);

        #region Unity Methods
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

                    ActiveColor = gradientLUT.EvaluateUnscaled((float)clampPower);
                }
            }
        }
        private void OnDestroy()
        {
            simulation.clamps.Remove(this);
        }
        #endregion

        #region Orientation
        private void SetScale(NDSimulation simulation, NeuronCell.NodeData cellNodeData)
        {
            currentVisualizationScale = (float)simulation.VisualInflation;

            float radiusScalingValue = radiusRatio * (float)cellNodeData.nodeRadius;
            float heightScalingValue = heightRatio * simulation.AverageDendriteRadius;

            //Ensures clamp is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            transform.parent.localScale = new Vector3(radiusLength, radiusLength, heightScalingValue);
            UpdateHighLightScale(transform.parent.localScale);
        }
        public void UpdateScale(float newScale)
        {
            if (this != null)
            {
                float modifiedScale = newScale / currentVisualizationScale;
                Vector3 tempVector = transform.parent.localScale;
                tempVector.x *= modifiedScale;
                tempVector.y *= modifiedScale;
                transform.parent.localScale = tempVector;
                currentVisualizationScale = newScale;
                UpdateHighLightScale(transform.parent.localScale);
                
            }
        }
        private void UpdateHighLightScale(Vector3 clampScale)
        {
            float max = Math.Max(clampScale) * highlightSphereScale;
            highlightObj.transform.localScale = new Vector3((1 / clampScale.x) * max, 
                (1 / clampScale.y) * max, 
                (1 / clampScale.z) * max);

            // If tbe clamp is too small, match a minimum global size
            if (highlightObj.transform.lossyScale.x < minHighlightGlobalSize)
            {
                Vector3 globalSize = new Vector3(minHighlightGlobalSize, minHighlightGlobalSize, minHighlightGlobalSize);
                Transform curParent = transform.parent;
                // Convert global size to local space
                do
                {
                    globalSize = new Vector3(globalSize.x / curParent.localScale.x,
                        globalSize.y / curParent.localScale.y,
                        globalSize.z / curParent.localScale.z);
                    curParent = curParent.parent;
                } while (curParent.parent != null);
                globalSize = new Vector3(globalSize.x / curParent.localScale.x,
                    globalSize.y / curParent.localScale.y,
                    globalSize.z / curParent.localScale.z);

                highlightObj.transform.localScale = globalSize;
                Debug.Log("Highlight size increased to match minimum");
            }

            Debug.Log("highlight global scale: " + highlightObj.transform.lossyScale.ToString("F5") + "\nlocal scale: " + highlightObj.transform.localScale.ToString("F5"));
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
        #endregion

        #region Simulation Checks
        /// <summary>
        /// Attempt to latcha  clamp onto a simulation object
        /// </summary>
        public NDSimulation ReportSimulation(RaycastHit hit)
        {
            var sim = hit.collider.GetComponent<NDSimulation>();
            if (sim == null) sim = hit.collider.GetComponentInParent<NDSimulation>();
            if (sim == null) return null;
            return ReportSimulation(sim, hit);
        }
        /// <summary>
        /// Attempt to latch a clamp onto a given simulation
        /// </summary>
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

                SetScale(this.simulation, clampCellNodeData);
                SetRotation(this.simulation, clampCellNodeData);

                simulation.OnVisualInflationChange += VisualInflationChangeHandler;

                gradientLUT = this.simulation.GetComponent<LUTGradient>();

                this.simulation.clamps.Add(this);

                transform.parent.localPosition = focusPos;
            }

            return this.simulation;
        }
        private int GetNearestPoint(NDSimulation simulation, RaycastHit hit)
        {
            // Translate contact point to local space

            MeshFilter mf = simulation.transform.GetComponentInParent<MeshFilter>();
            if (mf == null) return -1;

            // Get 3D mesh vertices from hit triangle
            int triInd = hit.triangleIndex * 3;
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Find 1D verts belonging to these 3D verts
            int[] verts1D = new int[]
            {
                simulation.Map[v1].v1, simulation.Map[v1].v2,
                simulation.Map[v2].v1, simulation.Map[v2].v2,
                simulation.Map[v3].v1, simulation.Map[v3].v2
            };
            Vector3 localHitPoint = simulation.transform.InverseTransformPoint(hit.point); 

            float nearestDist = float.PositiveInfinity;
            int nearestVert1D = -1;
            string s = "Finding nearest vert for clamp based on hit (" + localHitPoint.ToString("F5") +  ":";
            foreach(int vert in verts1D)
            {
                float dist = Vector3.Distance(localHitPoint, simulation.Verts1D[vert]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVert1D = vert;
                    s += "\n\tNew nearest vert: " + vert + "\n\t\tpos: " + simulation.Verts1D[nearestVert1D].ToString("F5") + "\n\t\tdist: " + nearestDist;
                }
            }
            Debug.Log(s);

            return nearestVert1D;
        }
        // Returns true if the that 1D index is available, otherwise returns false
        private bool VertIsAvailable(int clampIndex, NDSimulation simulation)
        {
            bool validLocation = true;
            // minimum distance between clamps 
            float distanceBetweenClamps = simulation.AverageDendriteRadius * heightRatio * 2;

           
            foreach (NeuronClamp clamp in simulation.clamps)
            {
                // If there is a clamp on that 1D vertex, the spot is not open
                if (clamp.focusVert == clampIndex)
                {
                    Debug.LogWarning("Clamp already exists on focus vert [" + clampIndex + "]");
                    validLocation = false;
                }
                // If there is a clamp within 2*clamp height, the spot is not open
                else
                {
                    Vector3[] verts = simulation.Verts1D; //expensive?
                    float dist = (verts[clamp.focusVert] - verts[clampIndex]).magnitude;
                    if (dist < distanceBetweenClamps)
                    {
                        Debug.LogWarning("Clamp too close to clamp located on vert [" + clamp.focusVert + "].");
                        string s = "clamp position: " + clamp.transform.parent.localPosition + "\nnew clamp position: " + transform.parent.localPosition +
                            "\nDistance between clamps: " + (clamp.transform.parent.localPosition - transform.parent.localPosition).magnitude +
                            "\nMax distance allowed: " + distanceBetweenClamps +
                            "\nClamp index: " + clampIndex;
                        Debug.Log(s);
                        validLocation = false;
                    }
                }
            }
            return validLocation;
        }
        #endregion

        #region Clamp Controls
        public void ActivateClamp()
        {
            clampLive = true;

            if (activeMaterial != null)
                mr.material = activeMaterial;
        }
        public void DeactivateClamp()
        {
            clampLive = false;
            if (inactiveMaterial != null)
                mr.material = inactiveMaterial;
        }
        public void ToggleClamp()
        {
            if (clampLive) DeactivateClamp();
            else ActivateClamp();
        }
        #endregion

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

        public void Highlight(bool highlight)
        {
            if (highlightObj != null)
                highlightObj.SetActive(highlight);
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
