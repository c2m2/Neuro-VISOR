using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction
{
    public class LoadSimulation : MonoBehaviour
    {
        public string vrnFileName = "10-dkvm2_1d";
        public Gradient gradient;
        private bool loaded = false;


        public void Load(RaycastHit hit)
        {
            if (!loaded)
            {
                GameObject solveObj = new GameObject();
                solveObj.name = "Solver";
                solveObj.AddComponent<MeshFilter>();
                solveObj.AddComponent<MeshRenderer>();
                NDSimulation solver = solveObj.AddComponent<SparseSolverTestv1>();
                solver.vrnFileName = vrnFileName;
                solver.gradient = gradient;
                solver.Initialize();

                loaded = true;
                transform.gameObject.SetActive(false);
            }
        }
    }
}