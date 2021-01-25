using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    public class SimulationLoader : MonoBehaviour
    {
        public string vrnFileName = "10-dkvm2_1d";
        public Gradient gradient;
        public float globalMin = float.PositiveInfinity;
        public float globalMax = float.NegativeInfinity;

        private bool loaded = false;
        public string solverType = "SparseSolverTestv1";

        public void Load(RaycastHit hit)
        {
            if (!loaded)
            {
                GameObject solveObj = new GameObject();
                solveObj.name = "Solver";
                solveObj.AddComponent<MeshFilter>();
                solveObj.AddComponent<MeshRenderer>();
                NDSimulation solver = solveObj.AddComponent<SparseSolverTestv1>();

                // Set solver values
                solver.vrnFileName = vrnFileName;
                solver.gradient = gradient;
                solver.globalMin = globalMin;
                solver.globalMax = globalMax;

                solver.Initialize();

                loaded = true;
                transform.gameObject.SetActive(false);
            }
        }
    }
}