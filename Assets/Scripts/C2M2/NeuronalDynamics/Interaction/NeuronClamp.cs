﻿using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Interaction.UI;
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

        public bool ClampLive { get; private set; } = false;

        public double ClampPower { get; set; } = double.PositiveInfinity;
        public NeuronClampManager ClampManager { get { return GameManager.instance.ndClampManager; } }
        public double MinPower { get { return ClampManager.MinPower; } }
        public double MaxPower { get { return ClampManager.MaxPower; } }

        // public int focusVert { get; private set; } = -1; // Me
        public int focusVert { get; set; } = -1;

        public Vector3 FocusPos
        {
            get { return simulation.Verts1D[focusVert]; }
        }

        public Neuron.NodeData NodeData
        {
            get
            {
                return simulation.Neuron.nodes[focusVert];
            }
        }

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;
        public Material previewMaterial = null;
        public Material destroyMaterial = null;

        public NDSimulation simulation = null;

        public List<GameObject> defaultCapHolders = null;
        public List<GameObject> destroyCapHolders = null;

        public InfoPanel clampInfo = null;

        private MeshRenderer mr;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }
        private Vector3 posFocus = Vector3.zero;

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
        private ColorLUT ColorLUT { get { return simulation.ColorLUT; } }

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
            ClampPower = (MaxPower - MinPower) / 2;
        }
        private void Update()
        {
            if(simulation != null)
            {
                if (ClampLive)
                {
                    Color newCol = ColorLUT.Evaluate((float)ClampPower);
                    ActiveColor = newCol;
                }
            }
        }
        private void OnDestroy()
        {
            lock (simulation.clampLock) simulation.clamps.Remove(this);
        }
        #endregion

        #region Appearance

        /// <summary>
        /// Sets the design of the clamp based on whether it is on the soma
        /// </summary>
        private void CheckSoma(Neuron.NodeData cellNodeData)
        {
            if (somaClamp)
            {
                //Lowers the highlight radius
                highlightSphereScale = 1.1f;
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

            //if (somaClamp) transform.parent.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
            transform.parent.localScale = new Vector3(radiusLength, radiusLength, heightScalingValue);
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
                //if (somaClamp) tempVector.z *= modifiedScale;
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
            List<int> neighbors = cellNodeData.AdjacencyList.Keys.ToList();

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
                rotationVector = simulation.Verts1D[cellNodeData.Pid] - simulation.Verts1D[cellNodeData.Id];
            }
            else
            {
                rotationVector = Vector3.up; //if a clamp has no neighbors or is soma it will use a default orientation of facing up
            }
            transform.parent.localRotation = Quaternion.LookRotation(rotationVector);
        }

        /// <summary>
        /// Shows a popup of clamp voltage and clamp vertex
        /// </summary>
        public void ShowClampInfo()
        {
            clampInfo.gameObject.SetActive(true);
            clampInfo.Vertex = focusVert;
            clampInfo.Power = ClampPower * simulation.unitScaler;
            clampInfo.FocusLocalPosition = transform.localPosition;
        }

        /// <summary>
        /// Hides a popup of clamp voltage and clamp vertex
        /// </summary>
        public void HideClampInfo()
        {
            clampInfo.gameObject.SetActive(false);
        }

        #endregion

        #region Simulation Checks
        /// <summary>
        /// Attempt to latch a clamp onto a given simulation
        /// </summary>
        public NDSimulation AttachSimulation(NDSimulation simulation, int clampIndex)
        {
            if (this.simulation == null)
            {
                this.simulation = simulation;

                transform.parent.parent = this.simulation.transform;

                if (this.simulation.Neuron.somaIDs.Contains(clampIndex)) somaClamp = true;


                PlaceClamp(clampIndex);

                this.simulation.OnVisualInflationChange += VisualInflationChangeHandler;

                // wait for clamp list access, add to list
                lock (simulation.clampLock) this.simulation.clamps.Add(this);
                
            }

            return this.simulation;
        }

        public void PlaceClamp(int clampIndex)
        {
            focusVert = clampIndex;

            //CheckSoma(NodeData);
            SetScale(NodeData);
            SetRotation(NodeData);

            transform.parent.localPosition = FocusPos;
        }
        #endregion

        #region Clamp Controls
        public void ActivateClamp()
        {
            ClampLive = true;

            SwitchMaterial(activeMaterial);
        }
        public void DeactivateClamp()
        {
            ClampLive = false;
            SwitchMaterial(inactiveMaterial);
        }

        public void SwitchMaterial(Material material)
        {
            if (material != null) mr.material = material;
        }

        public void ToggleClamp()
        {
            if (ClampLive) DeactivateClamp();
            else ActivateClamp();
        }
        #endregion

        #region Input
        int holdCount = 0;

        private bool powerClick = false;
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            if (ClampManager.PressedCancel)
            {
                ResetInput();
            }

            if (ClampManager.PressedToggleDestroy)
            {
                holdCount++;

                // If we've held the button long enough to destroy, color caps red until user releases button
                if(holdCount > ClampManager.destroyCount && !powerClick) SwitchCaps(false);
                else if (powerClick) SwitchCaps(true);
            }
            else CheckInput();

            float power = ClampManager.PowerModifier;
            
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !powerClick) powerClick = true;

            ClampPower += power;
            Math.Clamp(ClampPower, MinPower, MaxPower);
        }

        // Changes clamp to a red aesthetic to signal that destroy is imminent
        private void SwitchCaps(bool toDefault)
        {
            if (defaultCapHolders != null && destroyCapHolders != null)
            {
                foreach (GameObject defaultCapHolder in defaultCapHolders)
                {
                    defaultCapHolder.SetActive(toDefault);
                }
                foreach (GameObject destroyCapHolder in destroyCapHolders)
                {
                    destroyCapHolder.SetActive(!toDefault);
                }
                if (toDefault)
                {
                    if (ClampLive) SwitchMaterial(activeMaterial);
                    else SwitchMaterial(inactiveMaterial);
                }
                else SwitchMaterial(destroyMaterial);
            }
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
            if (!ClampManager.PressedCancel && !powerClick)
            {
                if (holdCount >= ClampManager.destroyCount)
                {
                    Destroy(transform.parent.gameObject);
                }
                else if (holdCount > 0) ToggleClamp();
            }

            holdCount = 0;
            powerClick = false;
            SwitchCaps(true);
        }

        #endregion
    }
}
