#pragma warning disable CS0618

using System.Collections;
using System;
using UnityEngine;

namespace C2M2.Interaction
{
    [Obsolete("Replaced by Simulation")]
    public class GaussianDiffusion : MonoBehaviour
    {
        public GaussianDistanceCalculator gaussian { get; private set; }
        private MeshInfo adjacencyListManager;
        private MeshInfo.DataChangeRequest[] dataChangeRequests;
        private DijkstraFindPath findPath;
        private float[] localMinDistances;
        private double gaussianHeight;
        private Guid id;
        private string idString;
        private string formatStringStart = "Beginning new diffusion[{0}] across distance of {1} at a rate of {2}/second ({3} seconds).";
        private string formatStringEnd = "Diffusion[{0}] finished";

        public void InitializeDiffusion(MeshInfo dijkstraObject, DijkstraFindPath findPath, double gaussianHeight, double distanceTreshold)
        {
            this.adjacencyListManager = dijkstraObject;
            this.findPath = findPath;
            localMinDistances = findPath.minDistances;
            gaussian = gameObject.AddComponent<GaussianDistanceCalculator>();
            gaussian.SetGaussian(gaussianHeight, distanceTreshold / 3.716922188f);
            //gaussian = new Gaussian(gaussianHeight, distanceTreshold / 3.716922188f);
            dataChangeRequests = new MeshInfo.DataChangeRequest[findPath.closestMeshVertArr.Length];
            for (int i = 0; i < findPath.closestMeshVertArr.Length; i++)
            { // Initialize our current and previous changes
                dataChangeRequests[i] = new MeshInfo.DataChangeRequest(findPath.closestMeshVertArr[i], 0, 0);
            }
            id = Guid.NewGuid();
            idString = id.ToString();
        }
        public void BeginDiffusion(float diffusionRate, float distanceThreshold)
        {
            StartCoroutine(Diffusion(diffusionRate, distanceThreshold));
        }
        /// If we specify that this should diffuse at 0.1 meters/seconds up to a distance of 1 meter, and we specify 24 frames/s,
        /// Our diffusion should take totalTime = distanceThreshold/diffusionRate = 10 seconds and it should take frames = totalTime * framesPerSecond = 240 frames
        public IEnumerator Diffusion(float diffusionRate, float distanceThreshold)
        {
            float framesPerSecond = 20;
            float totalTime = distanceThreshold / diffusionRate;
            float frames = totalTime * framesPerSecond;
            float secondsPerFrame = 1 / framesPerSecond;
            float distanceIncrement = distanceThreshold / frames;
            // Todo: Print origin point in this log item
            Debug.Log(string.Format(formatStringStart, idString, distanceThreshold, diffusionRate, totalTime));
            for (int i = 1; i <= frames; i++)
            { // For each frame, get the current distance and Gaussian width, then calculate the values for that distance/width
                float currentDistance = distanceIncrement * i;
                float currentWidth = currentDistance / 3.716922188f;
                gaussian.StdDev = currentWidth;
                CalculateDiffusionFrame(currentDistance);
                yield return new WaitForSeconds(secondsPerFrame);
            }
            Debug.Log(string.Format(formatStringEnd, idString));
            Destroy(this);
        }
        private void CalculateDiffusionFrame(float currentDistanceThreshold)
        {
            double curveHeight = gaussian.height;
            for (int i = 0; i < dataChangeRequests.Length; i++)
            { // Apply Gaussian to all closest vertices and their overlaps
                float curMinDistance = localMinDistances[dataChangeRequests[i].index];
                if (curMinDistance <= currentDistanceThreshold)
                { // If our point is within the current reasonable range, update the value. Otherwise do nothing
                    double curVal = gaussian.CalculateGaussian(curMinDistance);
                    dataChangeRequests[i].prevConcentration = dataChangeRequests[i].curConcentration;
                    dataChangeRequests[i].curConcentration = curVal;
                }

            }
            //dijkstraObject.SubmitComponentDataChanges(this, dataChangeRequests);
        }
    }
}
