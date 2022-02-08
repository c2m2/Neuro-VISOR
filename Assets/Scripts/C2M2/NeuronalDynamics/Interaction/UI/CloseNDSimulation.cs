using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class CloseNDSimulation : MonoBehaviour
    {
        public NDBoardController simController = null;
        public NDSimulation Sim
        {
            get
            {
                return GameManager.instance.simulationManager.ActiveSimulations[0];
            }
        }

        private void Awake()
        {
            if(simController == null)
            {
                simController = GetComponentInParent<NDBoardController>();
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
                // Destroy the cell's ruler
                Sim.CloseRuler();

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
