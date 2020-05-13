using UnityEngine;
using System;

namespace C2M2.Interaction
{
    using Adjacency;
    public class RaycastSimHeaterContinuous : RaycastSimHeater
    {
        private DijkstraSearch dijkstraSearch;
        private GaussianDistanceCalculator gaussian;
        private UniqueVertices uniqueVerts;

        protected override void OnAwake()
        {
            // TODO: This abstract class doesn't need anything below here
            dijkstraSearch = GetComponent<DijkstraSearch>() ?? gameObject.AddComponent<DijkstraSearch>();
            uniqueVerts = GetComponent<UniqueVertices>() ?? gameObject.AddComponent<UniqueVertices>();
            gaussian = gameObject.AddComponent<GaussianDistanceCalculator>();
            gaussian.height = 1;
            gaussian.StdDev = 0.01;
        }
        // TODO: Make this an abstract method to be overriden
        protected override Tuple<int, double>[] HitMethod(RaycastHit hit)
        {
            // Run Dijkstra search, get nearest verts back
            int[] nearest3DVerts = dijkstraSearch.Search(hit, gaussian.DistanceThreshold(0.001f));
            Tuple<int, double>[] valueChanges = new Tuple<int, double>[nearest3DVerts.Length];

            // Do Dijkstra search from hit point
            float dt = Time.deltaTime;
            // Value change = distance of current vert from origin put thru Gaussian   
            for (int i = 0; i < nearest3DVerts.Length; i++)
            {
                int ind = nearest3DVerts[i];
                // send the distance through the gaussian to get the value
                double newVal = dt * gaussian.CalculateGaussian(dijkstraSearch.minDistances[nearest3DVerts[i]]);
                valueChanges[i] = new Tuple<int, double>(ind, newVal);
            }
            return valueChanges;
        }
        public void SetWarmCoolHands()
        {
            //Color hotCol = colorManager.gradient.Evaluate(1);
            // Color coolCol = colorManager.gradient.Evaluate(0);
            //GameManager.instance.RaycasterRightChangeColor(hotCol);
            //GameManager.instance.RaycasterRightChangeColor(coolCol);
        }
        public void RaycastersDefaultColor()
        {
            GameManager.instance.RaycasterRightChangeColor(Color.white);
            GameManager.instance.RaycasterLeftChangeColor(Color.white);
        }
    }
}
