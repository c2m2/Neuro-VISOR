using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI {
    public class ClampModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggle)
        {

            if (toggle)
            {
                Debug.Log("Clamp mode toggled on");

                GameManager.instance.simulationManager.FeatState = NDSimulationManager.FeatureState.Clamp;
            }

            
            // Enable/Disable group clamp controllers
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                NeuronClampManager manager = GameManager.instance.simulationManager.ActiveSimulations[0].clampManager;
                if (manager.clampControllerL != null)
                {
                    manager.clampControllerL.SetActive(toggle);
                }
                if (manager.clampControllerR != null)
                {
                    manager.clampControllerR.SetActive(toggle);
                }
            }
        }
    }
}