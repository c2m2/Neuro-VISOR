using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI {
    public class ClampModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggle)
        {

            if (toggle)
            {
                GameManager.instance.simulationManager.FeatState = NDSimulationManager.FeatureState.Clamp;
            }

            
            // Enable/Disable group clamp controllers
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                if (GameManager.instance.clampManagerL != null)
                {
                    GameManager.instance.clampManagerL.SetActive(toggle);
                }
                if (GameManager.instance.clampManagerR != null)
                {
                    GameManager.instance.clampManagerR.SetActive(toggle);
                }
            }
        }
    }
}