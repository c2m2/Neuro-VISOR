using UnityEngine;
using C2M2.Utils;
using C2M2.NeuronalDynamics.Simulation;
using UnityEngine.UI;
namespace C2M2.Interaction.UI

{
    public class SaveButton : MonoBehaviour
    {
        private CSVWriter csv;
        private GameManager gm = null;
        private NDSimulation sim;
        

        private void Start()
        {
            gm = GameManager.instance;
            // sim = (SparseSolverTestv1)gm.activeSims[0];
        }
        public void StartCSV(bool single)
        {   //restrict user from saving multiple csv files, disable button after clicking
            UnityEngine.Debug.Log("In the Write");
            csv = GameManager.instance.activeSims[0].gameObject.AddComponent<CSVWriter>();
            sim = (NDSimulation)GameManager.instance.activeSims[0];
            sim.solver = (SparseSolverTestv1)gm.activeSims[0];
            sim.csv = csv;
            csv.single = single;
            
            
        }
        public void StopCSV()
        {
            Destroy(csv);
            sim = (NDSimulation)GameManager.instance.activeSims[0];
            sim.csv = null;
        }
    }

}