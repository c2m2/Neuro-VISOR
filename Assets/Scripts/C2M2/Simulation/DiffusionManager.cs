using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace C2M2.Simulation
{
    using Interaction;
    using Interaction.UI;
    [Obsolete("Replaced by Simulation")]
    public class DiffusionManager : MonoBehaviour
    {
        private void Awake()
        {
            CreateDiffusion();
        }
        [Header("Reaction Diffusion Controls")]
        /// <summary> U^* = U^n + (ΔU^n)dt; ΔU^n = diffusionConstant(U^n - V^n), where V is a vert that shares an edge with U" </summary>
        private double diffusionConstant = 1;
        public double DiffusionConstant
        {
            get { return diffusionConstant; }
            set { diffusionConstant = value;
                if (activeDiffusion != null) { activeDiffusion.diffusionConstant = diffusionConstant; }
                if (diffusionConstantInputField != null) { diffusionConstantInputField.placeHolder.text = diffusionConstant.ToString("F2"); }
            }
        }
        public RaycastInputField diffusionConstantInputField;
        ///<summary> U^{n+1} = (reactionConstant)(U^*)(U^* - beta)(1 - U^*) </summary>
        private double reactionConstant = 2;
        public double ReactionConstant
        {
            get { return reactionConstant; }
            set { reactionConstant = value;
                if (activeDiffusion != null) { activeDiffusion.reactionConstant = reactionConstant; }
                if (reactionConstantInputField != null) { reactionConstantInputField.placeHolder.text = reactionConstant.ToString("F2"); }
            }
        }
        public RaycastInputField reactionConstantInputField;
        ///<summary> U^{n+1} = (reactionConstant)(U^*)(U^* - beta)(1 - U^*) </summary>
        private double beta = 0.5;
        public double Beta
        {
            get { return beta; }
            set { beta = value;
                if (activeDiffusion != null) { activeDiffusion.beta = beta; }
                if (betaInputField != null) { betaInputField.placeHolder.text = beta.ToString("F2"); }
            }
        }
        public RaycastInputField betaInputField;
        public double gaussianHeight = 1;
        public double gaussianStdDev = 3;
        private ObjectManager objectManager = null;
        public GameObject pointInfoPrefab;
        public ReactionDiffusion activeDiffusion { get; private set; } = null;
        public GaussianValueController gaussAddCond { get; private set; } = null;
        public GaussianValueController gaussSubCond { get; private set; } = null;
        private Coroutine gaussAddCondRoutine;
        private Coroutine gaussSubCondRoutine;
        public void Initialize(ObjectManager objectManager)
        {
            this.objectManager = objectManager;
        }
        //private ObjectManager objectManager;      This should create the diffusionManager, the adjacencyListManager, the VTUManager
        public void CreateDiffusion()
        {
            if (activeDiffusion == null)
            {
                activeDiffusion = gameObject.AddComponent<ReactionDiffusion>();
                DiffusionConstant = diffusionConstant;
                ReactionConstant = reactionConstant;
                Beta = beta;
                if (objectManager != null) { activeDiffusion.Initialize(objectManager); }
            }
        }
        #region Gaussian_Warming_Cooling
        public void CreateGaussAddCond()
        { // Stop any previous warming process, create a new one, and start its routine
            StopGaussAddCond();
            gaussAddCond = new GaussianValueController(objectManager, true, gaussianHeight, gaussianStdDev);
            if (!gaussAddCond.started) { gaussAddCondRoutine = StartCoroutine(gaussAddCond.ChangeValueContinuous()); }
        }
        public void CreateGaussSubCond()
        { // Stop any previous cooling process, create a new one, and start its routine
            StopGaussSubCond();
            gaussSubCond = new GaussianValueController(objectManager, false, gaussianHeight, gaussianStdDev);
            if (!gaussSubCond.started) { gaussSubCondRoutine = StartCoroutine(gaussSubCond.ChangeValueContinuous()); }
        }
        public void PlayGaussAddCond(RaycastHit hit) { if (gaussAddCond != null) { gaussAddCond.UpdateHit(hit); } }
        public void PlayGaussSubCond(RaycastHit hit) { if (gaussSubCond != null) { gaussSubCond.UpdateHit(hit); } }
        public void StopGaussAddCond()
        {
            if (gaussAddCondRoutine != null)
            {
                StopCoroutine(gaussAddCondRoutine);
                gaussAddCondRoutine = null;
            }
            if (gaussAddCond != null) { gaussAddCond = null; }
        }
        public void StopGaussSubCond()
        {
            if (gaussSubCondRoutine != null)
            {
                StopCoroutine(gaussSubCondRoutine);
                gaussSubCondRoutine = null;
            }
            if (gaussSubCond != null) { gaussSubCond = null; }
        }
        #endregion
        public void PointInfoPanelInstantiate(RaycastHit hit)
        {
            GameObject instantiated = Instantiate(pointInfoPrefab, objectManager.meshInfo.uniqueVerts[objectManager.meshInfo.FindNearestUniqueVert(hit)], Quaternion.identity);
            instantiated.transform.parent = transform;
            instantiated.GetComponent<PointInfo>().InitializeInfoPanel(objectManager, hit);
        }
        #region Simulation_Controls
        public void DiffusionPlay() { if (activeDiffusion != null) { activeDiffusion.StartSimulation(); } }
        public void DiffusionPause() { if (activeDiffusion != null) { activeDiffusion.StopSimulation(); } }
        public void DiffusionInsertValue(int index, double value) { if (activeDiffusion != null) { activeDiffusion.AddDiffusionCondition(index, value); } }
        public void DiffusionValuesRandomize() { if (activeDiffusion != null) { activeDiffusion.ValuesRandomize(); } }
        public void DiffusionValuesReset() { if (activeDiffusion != null) { activeDiffusion.ValuesEmpty(); } }
        #endregion
        #region SetDiffusionConstants
        public void SetDiffusionConstant(string s) { if (activeDiffusion != null) { if (double.TryParse(s, out double d)) { diffusionConstant = d; } } }
        public void SetReactionConstant(string s) { if (activeDiffusion != null) { if (double.TryParse(s, out double r)) { reactionConstant = r; } } }
        public void SetBetaConstant(string s) { if (activeDiffusion != null) { if (double.TryParse(s, out double b)) { beta = b; } } }
        #endregion
    }
}
