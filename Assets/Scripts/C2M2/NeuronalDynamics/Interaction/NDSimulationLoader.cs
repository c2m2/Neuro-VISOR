using System;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using System.IO;
namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides an editor interface and method for loading simulations on demand.
    /// </summary>
    public class NDSimulationLoader : MonoBehaviour
    {
        /// <summary>
        /// Script name of the solver script.
        /// </summary>
        /// <remarks>
        /// Name must include full namespace, separated by '.' (i.e. 'C2M2.NeuronalDynamics.Simulation.SparseSolverTestv1')
        /// Script must be included in the assembly, this can be achieved by placing the script in the "Assets" folder.
        /// </remarks>
        [Tooltip("Script name of the solver script.")]
        public string solverName = "C2M2.NeuronalDynamics.Simulation.SparseSolverTestv1";
        public string vrnFileName { get; set; } = "null";
        public Gradient gradient;
        public float globalMin = float.PositiveInfinity;
        public float globalMax = float.NegativeInfinity;
        public string lengthScale = "μm";
        public int refinementLevel = 0;
        public double timestepSize = 0.002 * 1e-3;
        public double endTime = 100.0;
        public double raycastHitValue = 0.05;
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

        // TODO: Allow SparseSolverTestv1 to be a variable script
        public void Load(RaycastHit hit)
        {
            GameObject solveObj = new GameObject();
            solveObj.AddComponent<MeshFilter>();
            solveObj.AddComponent<MeshRenderer>();

            Type solverType = Type.GetType(solverName);
            if(solverType == null || !solverType.IsSubclassOf(typeof(NDSimulation)))
            {
                if(solverType == null) Debug.LogError(solverName + " could not be found.");
                else if(!solverType.IsSubclassOf(typeof(NDSimulation))) Debug.LogError(solverName + " is not a NDSimulation.");
                Destroy(solveObj);
                return;
            }

            solveObj.name = "(Solver)" + solverName.Substring(solverName.LastIndexOf('.') + 1);

            NDSimulation solver = (NDSimulation)solveObj.AddComponent(solverType);

            // Store the new active simulation
            GameManager.instance.activeSims.Add(solver);

            TransferValues();

            solver.Initialize();

            transform.gameObject.SetActive(false);

            void TransferValues()
            {
                // Set solver values
                solver.vrnFileName = vrnFileName;
                solver.gradient = gradient;
                solver.globalMin = globalMin;
                solver.globalMax = globalMax;
                solver.timeStep = timestepSize;
                solver.endTime = endTime;
                solver.raycastHitValue = raycastHitValue;
                solver.unit = unit;
                solver.unitScaler = unitScaler;
                solver.colorMarkerPrecision = colorScalePrecision;

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
            }
        }
    }
}