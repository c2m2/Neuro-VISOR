using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class CloseNDSimulation : MonoBehaviour
    {
        public NDSimulationController simController = null;
        public NDSimulation Sim
        {
            get
            {
                return simController.sim;
            }
        }

        private void Awake()
        {
            if(simController == null)
            {
                simController = GetComponentInParent<NDSimulationController>();
                if(simController == null)
                {
                    Debug.LogError("No simulation controller found.");
                    Destroy(this);
                }
            }
        }

        public void CloseSimulation()
        {
            if(Sim != null)
            {
                GameManager.instance.activeSims.Remove(Sim);

                // Destroy ruler if no cells are left
                // TODO See NDSimulationLoader for note on ruler generation and removal improvement
                if (GameManager.instance.activeSims.Count == 0) Destroy(GameObject.Find("Ruler"));

                // Destroy the cell
                Destroy(Sim.gameObject);

                // Destroy this control panel
                Destroy(transform.root.gameObject);

                if (GameManager.instance.cellPreviewer != null)
                {
                    // Reenable the cell previewer
                    GameManager.instance.cellPreviewer.SetActive(true);
                }
            }
        }
    }
}
