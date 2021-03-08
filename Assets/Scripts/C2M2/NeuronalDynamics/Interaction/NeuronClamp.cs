using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Visualization;
using Math = C2M2.Utils.Math;
using System.Linq;

namespace C2M2.NeuronalDynamics.Interaction
{
    [RequireComponent(typeof(MeshRenderer))]
    public class NeuronClamp : MonoBehaviour
    {
        public float radiusRatio = 3f;
        public float heightRatio = 1f;
        [Tooltip("The highlight sphere's radius is some real multiple of the clamp's radius")]
        public float highlightSphereScale = 3f;
        private bool somaClamp = false;

        public bool clampLive { get; private set; } = false;

        public double clampPower { get; set; } = double.PositiveInfinity;
        public NeuronClampManager ClampManager { get { return GameManager.instance.ndClampManager; } }
        public double MinPower { get { return ClampManager.MinPower; } }
        public double MaxPower { get { return ClampManager.MaxPower; } }

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
                inactiveMaterial.color = inactiveCol;
            }
        }
        private Color32 activeCol = Color.white;
        public Color32 ActiveColor
        {
            get { return activeCol; }
            private set
            {
                activeCol = value;
                mr.material.color = activeCol;
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

        private void Start()
        {
            clampPower = (MaxPower - MinPower) / 2;
        }
        private void FixedUpdate()
        {
            if(simulation != null)
            {
                if (clampLive)
                {
                    Color newCol = gradientLUT.Evaluate((float)clampPower);
                    ActiveColor = newCol;
                }
            }
        }
        private void OnDestroy()
        {
            simulation.clampMutex.WaitOne();
            simulation.clamps.Remove(this);
            simulation.clampMutex.ReleaseMutex();
        }
        #endregion

        #region Appearance

        /// <summary>
        /// Sets the design of the clamp based on whether it is on the soma
        /// </summary>
        private void SetAppearance(Neuron.NodeData cellNodeData)
        {
            if (somaClamp)
            {
                //Replaces mesh; no way to pull a primative mesh without generating its game object
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                transform.GetComponent<MeshFilter>().sharedMesh = tempSphere.GetComponent<MeshFilter>().mesh;
                Destroy(tempSphere);

                //Scales to normal sphere
                transform.localScale = Vector3.one;

                //Lowers the highlight radius
                highlightSphereScale = 1.1f;

                //Removes end caps
                Destroy(transform.GetChild(0).gameObject);
                Destroy(transform.GetChild(1).gameObject);
            }
        }

        /// <summary>
        /// Sets the radius and height of the clamp
        /// </summary>
        private void SetScale(Neuron.NodeData cellNodeData)
        {
            currentVisualizationScale = (float)simulation.VisualInflation;

            float radiusScalingValue = radiusRatio * (float)cellNodeData.NodeRadius;
            float heightScalingValue = heightRatio * simulation.AverageDendriteRadius;

            //Ensures clamp is always at least as wide as tall when Visual Inflation is 1
            float radiusLength = Math.Max(radiusScalingValue, heightScalingValue) * currentVisualizationScale;

            if (somaClamp) transform.parent.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
            else transform.parent.localScale = new Vector3(radiusLength, radiusLength, heightScalingValue);
            UpdateHighLightScale(transform.parent.localScale);
        }

        /// <summary>
        /// Scales the clamp by a scalar value
        /// </summary>
        public void UpdateScale(float newScale)
        {
            if (this != null)
            {
                float modifiedScale = newScale / currentVisualizationScale;
                Vector3 tempVector = transform.parent.localScale;
                tempVector.x *= modifiedScale;
                tempVector.y *= modifiedScale;
                if (somaClamp) tempVector.z *= modifiedScale;
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
            }
        }

        /// <summary>
        /// Sets the orientation of the clamp based on the surrounding neighbors
        /// </summary>
        public void SetRotation(Neuron.NodeData cellNodeData)
        {
            List<int> neighbors = cellNodeData.NeighborIDs;

            // Get each neighbor's Vector3 value
            List<Vector3> neighborVectors = new List<Vector3>();
            foreach (int neighbor in neighbors)
            {
                neighborVectors.Add(simulation.Verts1D[neighbor]);
            }

            Vector3 rotationVector;
            if (neighborVectors.Count == 2)
            {
                Vector3 clampToFirstNeighborVector = neighborVectors[0] - simulation.Verts1D[cellNodeData.Id];
                Vector3 clampToSecondNeighborVector = neighborVectors[1] - simulation.Verts1D[cellNodeData.Id];

                rotationVector = clampToFirstNeighborVector.normalized - clampToSecondNeighborVector.normalized;
            }
            else if (neighborVectors.Count > 0 && cellNodeData.Pid != -1) //Nodes with a Pid of -1 are somas
            {
                rotationVector = simulation.Verts1D[cellNodeData.Pid].normalized - transform.parent.localPosition.normalized;
            }
            else
            {
                rotationVector = Vector3.up; //if a clamp has no neighbors or is soma it will use a default orientation of facing up
            }
            transform.parent.localRotation = Quaternion.LookRotation(rotationVector);
        }
        #endregion

        #region Simulation Checks
        /// <summary>
        /// Attempt to latch a clamp onto a simulation object
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

                // Check for duplicates
                if (!VertIsAvailable(clampIndex, simulation))
                {
                    Destroy(transform.parent.gameObject);
                }

                Neuron.NodeData clampCellNodeData = simulation.Neuron.nodes[clampIndex];

                if(simulation.Neuron.somaIDs.Contains(clampIndex)) somaClamp = true;

                focusVert = clampIndex;
                focusPos = simulation.Verts1D[focusVert];

                SetAppearance(clampCellNodeData);
                SetScale(clampCellNodeData);
                SetRotation(clampCellNodeData);

                simulation.OnVisualInflationChange += VisualInflationChangeHandler;

                gradientLUT = this.simulation.GetComponent<LUTGradient>();

                // clamp can be added to simulation, wait for list access, add to list
                simulation.clampMutex.WaitOne();
                this.simulation.clamps.Add(this);
                simulation.clampMutex.ReleaseMutex();

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
            foreach(int vert in verts1D)
            {
                float dist = Vector3.Distance(localHitPoint, simulation.Verts1D[vert]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVert1D = vert;
                }
            }

            return nearestVert1D;
        }

        /// <returns> True if the 1D index is available, otherwise returns false</returns>
        private bool VertIsAvailable(int clampIndex, NDSimulation simulation)
        {
            bool validLocation = true;
            // minimum distance between clamps 
            float distanceBetweenClamps = simulation.AverageDendriteRadius * heightRatio * 2;
            Vector3[] verts = simulation.Verts1D; //expensive?

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
                    
                    float dist = (verts[clamp.focusVert] - verts[clampIndex]).magnitude;
                    if (dist < distanceBetweenClamps)
                    {
                        Debug.LogWarning("Clamp too close to clamp located on vert [" + clamp.focusVert + "].");
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

        private bool powerClick = false;
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            if (ClampManager.PressedToggleDestroy)
                holdCount++;
            else
                CheckInput();

            float power = ClampManager.PowerModifier;
            
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !powerClick) powerClick = true;

            clampPower += power;
            Math.Clamp(clampPower, MinPower, MaxPower);
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
