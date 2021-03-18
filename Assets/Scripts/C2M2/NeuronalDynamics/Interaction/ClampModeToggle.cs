using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction.UI;
namespace C2M2.NeuronalDynamics.Interaction {
    public class ClampModeToggle : NDToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggle)
        {
            // Check for valid simulation, buttons
            if (sim == null)
            {
                Debug.LogError("No simulation given to NDClampModeButtonController!");
                return;
            }

            if (toggle)
            {
                Debug.Log("Clamp mode toggled on");
                sim.raycastEventManager.LRTrigger = GameManager.instance.ndClampManager.hitEvent;
            }

            // Enablle/Disable group clamp controllers
            if (GameManager.instance.ndClampManager.clampControllerL != null)
            {
                GameManager.instance.ndClampManager.clampControllerL.SetActive(toggle);
            }
            if (GameManager.instance.ndClampManager.clampControllerR != null)
            {
                GameManager.instance.ndClampManager.clampControllerR.SetActive(toggle);
            }
        }
    }
}