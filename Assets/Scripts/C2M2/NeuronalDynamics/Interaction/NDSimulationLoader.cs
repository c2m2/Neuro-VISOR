using System;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using System.IO;
using System.Collections.Generic;
using C2M2.Interaction;
using System.Linq;
using C2M2.SomaPositionCalculation;

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
        private SomaPositionCalculator calculator = null; // Placeholder default calculator
        public string vrnFileName { get; set; } = "null";
        public float globalMin = -0.1f;
        public float globalMax = 0.1f;
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
                    sims.Add((NDSimulation)GameManager.instance.activeSims[i]);
                }
                return sims;
            }
        }

        public Vector3 rulerInitPos = new Vector3(-0.5f, 0.443f, -0.322f);
        public Vector3 rulerInitRot = new Vector3(90, 0, 0);

        private void Start()
        {
            calculator = new SomaPositionCalculator();
        }

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

            TransferValues();

            // make Save button visible
            Menu m = FindObjectOfType<Menu>();
            m.SaveButtonVisible(true);

            solver.Initialize();

            // Store the new active simulation after initialization completes
            lock (GameManager.instance.activeSimsLock)
            {
                GameManager.instance.activeSims.Add(solver);
            }

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
                
                GameObject[] pivotObjList = GameObject.FindGameObjectsWithTag("NeuronPivotPoint");
                if (pivotObjList.Count() == 0)
                {
                    GameObject pivotObj = Instantiate(Resources.Load("Prefabs/NeuronPivotPoint") as GameObject);
                    pivotObj.transform.SetParent(GameManager.instance.simulationSpace.transform);
                    pivotObj.transform.position = GameManager.instance.simulationSpace.transform.position;
                    pivotObj.transform.eulerAngles = GameManager.instance.simulationSpace.transform.eulerAngles;
                    pivotObj.name = "NeuronPivotPoint";
                    pivotObj.GetComponent<GrabRescaler>().target = GameManager.instance.simulationSpace.transform;
                }
            }
            // Make characteristic distance from mesh size
            Vector3 solverSize = solver.GetComponent<MeshRenderer>().bounds.size;
            solver.characteristicDistance = new CharacteristicDistance(solverSize, solver.transform);

            solveObj.transform.parent = GameManager.instance.simulationSpace.transform;
            solver.transform.localScale = Vector3.one;
            
            // Using the characteristic distance, calculate the best next soma position in the room
            solver.transform.position = (Vector3)calculator.CalculateNextSomaPosition(solver);
            calculator.SetRandomYRotation(solver.transform); // Set a random Y rotation for the solver

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
            solveObj.tag = "SimulatedNeuronCell";
            return solveObj;
        }
    }
}
