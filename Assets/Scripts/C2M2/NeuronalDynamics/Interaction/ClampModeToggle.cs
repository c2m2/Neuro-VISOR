using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction.UI;
namespace C2M2.NeuronalDynamics.Interaction {
    public class ClampModeToggle : NDFeatureToggle
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

                if(sim.raycastEventManager == null)
                {
                    Debug.LogError("No raycast event manager on simualtion");
                    return;
                }
                if(GameManager.instance.ndClampManager == null)
                {
                    Debug.LogError("No clamp manager attached to GameManager");
                    return;
                }
                if(GameManager.instance.ndClampManager.hitEvent == null)
                {
                    Debug.LogError("No hit event on clampmanager");
                    return;
                }
                sim.raycastEventManager.LRTrigger = GameManager.instance.ndClampManager.hitEvent;
            }

            // Enablle/Disable group clamp controllers
            if (GameManager.instance.VrIsActive)
            {
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
}