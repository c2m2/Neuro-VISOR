using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;
namespace C2M2.NeuronalDynamics.Interaction {
    public class NDClampModeButtonController : MonoBehaviour
    {
        public NDSimulation sim = null;
        public NDClampModeButton enabledButton = null;
        public NDClampModeButton disabledButton = null;

        private void Start()
        {
            // Check for valid simulation, buttons
            if(sim == null)
            {
                Debug.LogError("No simulation given to NDClampModeButtonController!");
                Destroy(this);
            }
            if(enabledButton == null || disabledButton == null)
            {
                Debug.LogError("No buttons found for NDClampModeSwitch!");
                Destroy(this);
            }

            // Pass simulation down to buttons
            enabledButton.sim = sim;
            disabledButton.sim = sim;

            // If ClampMode is initially enabled, show the enabled button and not the disabled button
            enabledButton.gameObject.SetActive(sim.ClampMode);
            disabledButton.gameObject.SetActive(!sim.ClampMode);
        }

    }
}