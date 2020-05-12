using System.Collections;
using UnityEngine;
using System;
namespace C2M2
{
    using Utils;
    using static Utils.Math;
    /// <summary> Set a Gaussian width and height, select an area on simulation mesh to add Gaussian values to scaled with time </summary>
    [Obsolete("Replaced by RaycastValueController")]
    public class GaussianValueController
    {
        // Deprecated
        private ObjectManager objectManager;
        [Tooltip("Should this value controller add or subtract value from our scalar field?")]
        // Deprecated
        public bool playFrame = false;
        // Deprecated
        public bool started = false;
        private bool warming = true;
        private GaussianDistanceCalculator gaussian;
        // Deprecated
        private double[] componentData;
        private DijkstraFindPath findPath;
        private double distanceThreshold;

        private RaycastHit curHit;
        private bool overrideDt = false;
        /// Workflow:
        ///  
        ///     1. Create a new array of scalar double values the length of AdjacencyList.componentData
        ///     2. Have a method that receives a raycast input which centers a Gaussian of given heigh and width at the raycast hit point
        ///     3. Calculate a Gaussian value for each point and scale it by dt
        ///         - Do we need to have an array the size of componentData? 
        ///         - How are we using FindPath, and what distance threshold should we use?
        ///             - Will this be fast enough?
        ///     4. Submit array of scalar doubles to AdjacencyList.activeDiffusion to incorporate changes into the diffusion
        public GaussianValueController(ObjectManager objectManager, bool warming, double gaussianHeight, double gaussianStdDev)
        {
            this.objectManager = objectManager;
            this.warming = warming;
            distanceThreshold = gaussianStdDev * 3.716922188f;
            gaussian = objectManager.gameObject.AddComponent<GaussianDistanceCalculator>();
            gaussian.SetGaussian(gaussianHeight, gaussianStdDev);
            //gaussian = new Gaussian(gaussianHeight, gaussianStdDev);
            findPath = new DijkstraFindPath(objectManager);

        }
        public GaussianValueController(bool warming, double gaussianHeight, double gaussianStdDev)
        {
            this.warming = warming;
            distanceThreshold = gaussianStdDev * 3.716922188f;
            gaussian = objectManager.gameObject.AddComponent<GaussianDistanceCalculator>();
            gaussian.SetGaussian(gaussianHeight, gaussianStdDev);
            //gaussian = new Gaussian(gaussianHeight, gaussianStdDev);
            findPath = new DijkstraFindPath(objectManager);
        }
        double dt = 0.1;
        // TODO: hardsetting dt is kinda dumb. We should just delete the GaussianValueControl when we stop pressing
        public IEnumerator ChangeValueContinuous()
        {
            double[] valueChanges = new double[0];
            MeshInfo.DNode[] newConditions = new MeshInfo.DNode[0];
            DateTime timeNow = DateTime.Now;
            DateTime timeOld;
            TimeSpan dtSpan;
            started = true;
            // TODO: We need a time paused
            while (true)
            {
                yield return new WaitUntil(() => playFrame);  // Wait until outside forces call for another frame to be played
                playFrame = false;
                if (objectManager.diffusionManager.activeDiffusion != null)
                {
                    // If our value changing array isn't the same length as our exiting values, create a new array
                    if (valueChanges.Length != objectManager.diffusionManager.activeDiffusion.simulationConditions.Length)
                    {
                        valueChanges = new double[objectManager.diffusionManager.activeDiffusion.simulationConditions.Length];
                    }
                    valueChanges.FillArray(0);
                    findPath.FindPath(curHit, (float)distanceThreshold);
                    //if (newConditions.Length != findPath.closestMeshVertArr.Length) { newConditions = new MeshInfo.NodeDouble[findPath.closestMeshVertArr.Length]; }
                    newConditions = new MeshInfo.DNode[findPath.closestMeshVertArr.Length];
                    timeOld = timeNow;
                    timeNow = DateTime.Now;
                    if (!overrideDt)
                    { // If we aren't getting our dt from a simulation
                        dtSpan = timeNow.Subtract(timeOld);
                        dt = dtSpan.TotalSeconds;
                        dt = Max(dt, 0.1); // dt <= 0.1
                    }
                    if (!warming) { dt = -dt; } // Effectively multiplies each new condition by -1, if we are cooling
                    for (int i = 0; i < findPath.closestMeshVertArr.Length; i++)
                    {
                        valueChanges[findPath.closestMeshVertArr[i]] = dt * gaussian.CalculateGaussian(findPath.minDistances[findPath.closestMeshVertArr[i]]);
                    }
                    objectManager.diffusionManager.activeDiffusion.ChangeValues(valueChanges);
                }
            }
        }
        public IEnumerator ControlValue()
        {
            double[] valueChanges = new double[0];
            MeshInfo.DNode[] newConditions = new MeshInfo.DNode[0];
            DateTime timeNow = DateTime.Now;
            DateTime timeOld;
            TimeSpan dtSpan;
            started = true;
            // TODO: We need a time paused
            while (true)
            {
                // If our value changing array isn't the same length as our exiting values, create a new array
                if (valueChanges.Length != objectManager.diffusionManager.activeDiffusion.simulationConditions.Length)
                {
                    valueChanges = new double[objectManager.diffusionManager.activeDiffusion.simulationConditions.Length];
                }
                valueChanges.FillArray(0);
                findPath.FindPath(curHit, (float)distanceThreshold);
                //if (newConditions.Length != findPath.closestMeshVertArr.Length) { newConditions = new MeshInfo.NodeDouble[findPath.closestMeshVertArr.Length]; }
                newConditions = new MeshInfo.DNode[findPath.closestMeshVertArr.Length];
                timeOld = timeNow;
                timeNow = DateTime.Now;
                if (!overrideDt)
                { // If we aren't getting our dt from a simulation
                    dtSpan = timeNow.Subtract(timeOld);
                    dt = dtSpan.TotalSeconds;
                    dt = Max(dt, 0.1); // dt <= 0.1
                }
                if (!warming) { dt = -dt; } // Effectively multiplies each new condition by -1, if we are cooling
                for (int i = 0; i < findPath.closestMeshVertArr.Length; i++)
                {
                    valueChanges[findPath.closestMeshVertArr[i]] = dt * gaussian.CalculateGaussian(findPath.minDistances[findPath.closestMeshVertArr[i]]);
                }
                objectManager.diffusionManager.activeDiffusion.ChangeValues(valueChanges);
            }
        }
        public void UpdateHit(RaycastHit hit)
        {
            curHit = hit;
            if (objectManager.diffusionManager.activeDiffusion.paused || !objectManager.diffusionManager.activeDiffusion.started) { PlayFrame(); }  // If our diffusion is paused or hasn't been started yet, play a frame
        }
        public void PlayFrame()
        {
            playFrame = true;
        }
        public void PlayFrame(double dt)
        {
            this.dt = dt;
            overrideDt = true;
            playFrame = true;
        }
    }
}
