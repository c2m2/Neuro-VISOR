using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Visualization;
namespace C2M2.Tests {
    public class PlotTests : MonoBehaviour
    {
        public GameObject graphPrefab = null;

        public bool runSinTest = true;
        public Vector3 sinTestPosition = Vector3.zero;
        public bool runCosTest = true;
        public Vector3 cosTestPosition = new Vector3(0.6f, 0f, 0f);
        public bool runConstantTest = true;
        public float constant = 4f;
        public Vector3 constantTestPosition = new Vector3(-0.6f, 0f, 0f);
        

        private void Awake()
        {
            if (graphPrefab == null) return;
            if(runSinTest) SinTest(sinTestPosition);
            if (runCosTest) CosTest(cosTestPosition);
            if (runConstantTest) ConstantTest(constantTestPosition);
        }

        private LineGrapher InstantiateGraph(Vector3 position)
        {
            GameObject go = (GameObject)Instantiate(graphPrefab, transform);
            go.transform.localPosition = position;
            LineGrapher graph = go.GetComponent<LineGrapher>();
            if (graph == null) Debug.LogError("No LineGrapher found on " + go);
            return graph;
        }
        private void SinTest(Vector3 position)
        {
            LineGrapher graph = InstantiateGraph(position);
            graph.name = "SinPlotTest";
            graph.MaxSamples = 250;
            graph.SetLabels("Sin vs. Time", "Time", "Sin(Time)");

            graph.YMax = 1;
            graph.YMin = -1;

            StartCoroutine(SinTest(graph));

            IEnumerator SinTest(LineGrapher sinGraph)
            {
                yield return new WaitUntil(() => Time.time > 0);
                while (true)
                {
                    yield return new WaitForFixedUpdate();
                    sinGraph.AddValue(Mathf.Sin(Time.time), Time.time);
                }
            }
        }
        private void CosTest(Vector3 position)
        {
            LineGrapher graph = InstantiateGraph(position);
            graph.name = "CosPlotTest";
            graph.MaxSamples = 250;
            graph.SetLabels("Cos vs. Time", "Time", "Cos(Time)");

            graph.YMax = 1;
            graph.YMin = -1;

            StartCoroutine(CosTest(graph));

            IEnumerator CosTest(LineGrapher cosGraph)
            {
                yield return new WaitUntil(() => Time.time > 0);
                while (true)
                {
                    yield return new WaitForFixedUpdate();
                    cosGraph.AddValue(Mathf.Cos(Time.time), Time.time);
                }
            }
        }

        private void ConstantTest(Vector3 position)
        {
            LineGrapher graph = InstantiateGraph(position);
            graph.name = "ConstPlotTest";
            graph.MaxSamples = 250;
            graph.SetLabels("Constant", "Time", "Y");

            graph.YMax = constant * 2;
            graph.YMin = 0;

            StartCoroutine(ConstantTest(graph));
            IEnumerator ConstantTest(LineGrapher constantGraph)
            {
                yield return new WaitUntil(() => Time.time > 0);
                while (true)
                {
                    yield return new WaitForFixedUpdate();
                    constantGraph.AddValue(constant, Time.time);
                }
            }
        }
    }
}