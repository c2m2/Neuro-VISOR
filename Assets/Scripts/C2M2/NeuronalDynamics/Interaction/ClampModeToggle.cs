using UnityEngine;
namespace C2M2.NeuronalDynamics.Interaction.UI {
    public class ClampModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggle)
        {
            if (toggle)
            {
                Debug.Log("Clamp mode toggled on");

                if(Sim.raycastEventManager == null)
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
                Sim.raycastEventManager.LRTrigger = GameManager.instance.ndClampManager.hitEvent;
            }

            // Enablle/Disable group clamp controllers
            if (GameManager.instance.vrDeviceManager.VRActive)
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