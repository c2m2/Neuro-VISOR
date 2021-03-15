using System;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides an editor interface and method for loading simulations on demand.
    /// </summary>
    public class NDSimulationLoader : MonoBehaviour
    {
        public string vrnFileName { get; set; } = "null";
        public Gradient gradient;
        public float globalMin = float.PositiveInfinity;
        public float globalMax = float.NegativeInfinity;
        public int refinementLevel = 0;
        public double timestepSize = 0.002 * 1e-3;
        public double endTime = 100.0;
        public double raycastHitValue = 0.05;

        public string solverType = "SparseSolverTestv1";
        /// <summary>
        /// Unit display string that can be manually set by the user
        /// </summary>
        [Tooltip("Unit display string that can be manually set by the user")]
        public string unit = "mV";
        /// <summary>
        /// Can be used to manually convert Gradient Display values to match unit string
        /// </summary>
        [Tooltip("Can be used to manually convert Gradient Display values to match unit string")]
        public float unitScaler = 1000f;
        /// <summary>
        /// Alter the precision of the color scale display
        /// </summary>
        [Tooltip("Alter the precision of the color scale display")]
        public int colorScalePrecision = 3;

        public void Load(RaycastHit hit)
        {
            GameObject solveObj = new GameObject();
            solveObj.name = solverType + "(Solver)";
            solveObj.AddComponent<MeshFilter>();
            solveObj.AddComponent<MeshRenderer>();
            NDSimulation solver = solveObj.AddComponent<SparseSolverTestv1>();

            // Close current simulation, if any
            if(GameManager.instance.activeSim != null)
            {
                Destroy(GameManager.instance.activeSim);
            }
            // Store the new active simulation
            GameManager.instance.activeSim = solver;

            // Set solver values
            solver.vrnFileName = vrnFileName;
            solver.gradient = gradient;
            solver.globalMin = globalMin;
            solver.globalMax = globalMax;
            solver.k = timestepSize;
            solver.endTime = endTime;
            solver.raycastHitValue = raycastHitValue;
            solver.unit = unit;
            solver.unitScaler = unitScaler;
            solver.colorScalePrecision = colorScalePrecision;

            try
            {
                solver.RefinementLevel = refinementLevel;              
            }
            catch (Exception e)
            {
                Debug.LogWarning("Refinement level " + refinementLevel + " not found. Reverting to 0 refinement.");
                refinementLevel = 0;
                solver.RefinementLevel = 0;
                Debug.LogError(e);
            }

            solver.Initialize();

            transform.gameObject.SetActive(false);
        }
    }
}