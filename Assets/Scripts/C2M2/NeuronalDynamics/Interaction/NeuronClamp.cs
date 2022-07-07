using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Interaction.UI;
using C2M2.Visualization;
using Math = C2M2.Utils.Math;
using System.Linq;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClamp : NDInteractables
    {
        public float MinPower { get { return simulation.ColorLUT.GlobalMin; } }
        public float MaxPower { get { return simulation.ColorLUT.GlobalMax; } }
        // Sensitivity of the clamp power control. Lower sensitivity means clamp power changes more quickly
        public float sensitivity = 5;
        public float Scaler { get { return (MaxPower - MinPower) / sensitivity; } }

        public float radiusRatio = 3f;
        public float heightRatio = 1f;
        [Tooltip("The highlight sphere's radius is some real multiple of the clamp's radius")]
        public float highlightSphereScale = 3f;
        private bool somaClamp = false;

        public bool ClampLive { get; private set; } = false;

        public double ClampPower { get; set; } = 0;
        public NeuronClampManager ClampManager { get { return simulation.clampManager; } }

        public Neuron.NodeData NodeData
        {
            get
            {
                return simulation.Neuron.nodes[FocusVert];
            }
        }
        
        public Material inactiveMaterial = null;

        public List<GameObject> capHolders = null;

        public Color defaultCapColor = Color.black;
        public Color destroyCapColor = Color.red;

        public InfoPanel clampInfo = null;
        
        public Color32 CurrentColor
        {
            get { return meshRenderer.material.color; }
            private set
            {
                meshRenderer.material.color = value;
            }
        }
        private ColorLUT ColorLUT { get { return simulation.ColorLUT; } }

        float currentVisualizationScale = 1;
        private void VisualInflationChangeHandler(double newInflation)
        {
            UpdateScale((float)newInflation);
        }

        public float minHighlightGlobalSize = 0.1f * (1/3);

        #region Unity Methods

        private void Start()
        {
            // only assign ClampPower if it's not loading; otherwise it will overwrite the loaded value
            if (!GameManager.instance.Loading)
            {
                ClampPower = (MaxPower - MinPower) / 2;
                UpdateColor();
            }
        }
        private void OnDestroy()
        {
            lock (simulation.clampLock) ClampManager.clamps.Remove(this);
        }
        #endregion

        #region Appearance

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

            if (somaClamp) transform.localScale = new Vector3(radiusLength, radiusLength, radiusLength);
            else transform.localScale = new Vector3(radiusLength, radiusLength, heightScalingValue);
            UpdateHighLightScale(transform.localScale);
        }

        /// <summary>
        /// Scales the clamp by a scalar value
        /// </summary>
        public void UpdateScale(float newScale)
        {
            if (this != null)
            {
                float modifiedScale = newScale / currentVisualizationScale;
                Vector3 tempVector = transform.localScale;
                tempVector.x *= modifiedScale;
                tempVector.y *= modifiedScale;
                if (somaClamp) tempVector.z *= modifiedScale;
                transform.localScale = tempVector;
                currentVisualizationScale = newScale;
                UpdateHighLightScale(transform.localScale);
                
            }
        }
        private void UpdateHighLightScale(Vector3 clampScale)
        {
            float max = Math.Max(clampScale) * highlightSphereScale;
            highlightObj.transform.localScale = new Vector3((1 / clampScale.x) * max, 
                (1 / clampScale.y) * max, 
                (1 / clampScale.z) * max);

            // If the clamp is too small, match a minimum global size
            if (highlightObj.transform.lossyScale.x < minHighlightGlobalSize)
            {
                highlightObj.transform.localScale *= minHighlightGlobalSize/highlightObj.transform.lossyScale.x;
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
            transform.localRotation = Quaternion.LookRotation(rotationVector);
        }

        /// <summary>
        /// Shows a popup of clamp voltage and clamp vertex
        /// </summary>
        public void ShowClampInfo()
        {
            clampInfo.gameObject.SetActive(true);
            clampInfo.Vertex = FocusVert;
            clampInfo.Power = ClampPower * simulation.unitScaler;
        }

        /// <summary>
        /// Hides a popup of clamp voltage and clamp vertex
        /// </summary>
        public void HideClampInfo()
        {
            clampInfo.gameObject.SetActive(false);
        }

        public void UpdateColor()
        {
            if (ClampLive)
            {
                Color newCol = ColorLUT.Evaluate((float)ClampPower);
                CurrentColor = newCol;
            }
        }

        #endregion
        
        override public void Place(int clampIndex)
        {
            if(simulation.Neuron.somaIDs.Contains(clampIndex)) somaClamp = true;
            
            simulation.OnVisualInflationChange += VisualInflationChangeHandler;

            // wait for clamp list access, add to list
            lock(simulation.clampLock) ClampManager.clamps.Add(this);

            if(somaClamp)
            {
                //Lowers the highlight radius
                highlightSphereScale = 1.1f;
            }
            SetScale(NodeData);
            SetRotation(NodeData);

            transform.localPosition = FocusPos;
        }

        #region Clamp Controls
        public void ActivateClamp()
        {
            ClampLive = true;

            SwitchMaterial(defaultMaterial);
            UpdateColor();
        }
        public void DeactivateClamp()
        {
            ClampLive = false;
            SwitchMaterial(inactiveMaterial);
        }

        public void ToggleClamp()
        {
            if (ClampLive) DeactivateClamp();
            else ActivateClamp();
        }
        #endregion

        #region Input
        
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            ClampManager.HoldCount += Time.deltaTime;

            // If we've held the button long enough to destroy, color caps red until user releases button
            if (ClampManager.HoldCount > ClampManager.DestroyCount && !ClampManager.PowerClick) SwitchCaps(false);
            else if (ClampManager.PowerClick) SwitchCaps(true);

            float power = Time.deltaTime*ClampManager.PowerModifier*Scaler;
            
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !ClampManager.PowerClick) ClampManager.PowerClick = true;

            ClampPower += power;
            UpdateColor();
        }

        private void CheckInput()
        {
            if (!ClampManager.PowerClick)
            {
                if (ClampManager.HoldCount >= ClampManager.DestroyCount)
                {
                    Destroy(gameObject);
                }
                else if (ClampManager.HoldCount > 0) ToggleClamp();
            }

            ClampManager.HoldCount = 0;
            ClampManager.PowerClick = false;
            SwitchCaps(true);
        }

        // Changes clamp to a red aesthetic to signal that destroy is imminent
        private void SwitchCaps(bool toDefault)
        {
            if (capHolders != null)
            {
                foreach (GameObject capHolder in capHolders)
                {
                    foreach(MeshRenderer cap in capHolder.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (toDefault) cap.material.color = defaultCapColor;
                        else cap.material.color = destroyCapColor;
                    }
                    
                }
                if (toDefault)
                {
                    if (ClampLive)
                    {
                        SwitchMaterial(defaultMaterial);
                        UpdateColor();
                    }
                    else SwitchMaterial(inactiveMaterial);
                }
                else SwitchMaterial(destroyMaterial);
            }
        }

        protected override void AddHitEventListeners()
        {
            HitEvent.OnHover.AddListener((hit) => ShowClampInfo());
            HitEvent.OnHoverEnd.AddListener((hit) => HideClampInfo());
            HitEvent.OnHoldPress.AddListener((hit) => MonitorInput());
            HitEvent.OnEndPress.AddListener((hit) => CheckInput());
        }

        #endregion
    }
}
