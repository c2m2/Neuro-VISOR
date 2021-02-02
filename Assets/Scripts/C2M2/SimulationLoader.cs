using System;
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
        public int refinementLevel = 0;

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
                try
                {
                    solver.RefinementLevel = refinementLevel;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Refinement level " + refinementLevel + " not found. Reverting to 0 refinement.");
                    refinementLevel = 0;
                    solver.RefinementLevel = 0;
                }

                solver.Initialize();


                loaded = true;
                transform.gameObject.SetActive(false);
            }
        }
    }
}