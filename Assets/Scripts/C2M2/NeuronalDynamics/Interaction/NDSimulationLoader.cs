using System;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using System.IO;
using System.Collections.Generic;
using C2M2.Interaction;
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

        // Casts GameManager's list of simulations as NDSimulations
        public List<NDSimulation> Sims
        {
            get
            {
                List<NDSimulation> sims = new List<NDSimulation>(GameManager.instance.activeSims.Count);
                for(int i = 0; i < GameManager.instance.activeSims.Count; i++)
                {
                    sims[i] = (NDSimulation)GameManager.instance.activeSims[i];
                }
                return sims;
            }
        }

        public Vector3 rulerInitPos = new Vector3(-0.5f, 0.443f, -0.322f);
        public Vector3 rulerInitRot = new Vector3(90, 0, 0);

        // TODO: Allow SparseSolverTestv1 to be a variable script
        public GameObject Load(RaycastHit hit)
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
                return null;
            }

            // The name of the object should take the form "[cellName](solverType)"
            solveObj.name = "Cell:[" + vrnFileName + "] Solver:(" + solverName.Substring(solverName.LastIndexOf('.') + 1) + ")";

            NDSimulation solver = (NDSimulation)solveObj.AddComponent(solverType);

            // Store the new active simulation
            GameManager.instance.activeSims.Add(solver);

            TransferValues();

            // make Save button visible
            Menu m = FindObjectOfType<Menu>();
            m.SaveButtonVisible(true);

            solver.Initialize();

            solver.Manager.FeatState = solver.Manager.FeatState;

            transform.gameObject.SetActive(false);
            
            if (GameManager.instance.activeSims.Count == 1) //no other sims present
            {
                GameManager.instance.simulationSpace.transform.localScale = solver.transform.localScale;

                // Instantiate ruler when the first simulation is added
                // TODO create better method of handling object generation and removal for ruler and similar objects
                GameObject rulerObj = Instantiate(Resources.Load("Prefabs/Ruler") as GameObject);
                rulerObj.transform.position = rulerInitPos;
                rulerObj.transform.eulerAngles = rulerInitRot;
                rulerObj.name = "Ruler";
                rulerObj.GetComponent<GrabRescaler>().target = GameManager.instance.simulationSpace.transform;
            }
            solveObj.transform.parent = GameManager.instance.simulationSpace.transform;
            solver.transform.localScale = Vector3.one;

            void TransferValues()
            {
                // Set solver values
                solver.vrnFileName = vrnFileName;
                solver.GlobalMin = globalMin;
                solver.GlobalMax = globalMax;
                solver.timeStep = timestepSize;
                solver.endTime = endTime;
                solver.raycastHitValue = raycastHitValue;
                solver.unit = unit;
                solver.unitScaler = unitScaler;

                solver.simID = GameManager.simID; // set ID

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

            return solveObj;
        }
    }
}
