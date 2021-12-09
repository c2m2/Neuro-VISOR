using UnityEngine;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class SynapseModeToggle : NDFeatureToggle
    {
        public override void OnToggle(RaycastHit hit, bool toggle)
        {
            if (toggle)
            {
                Debug.Log("Synapse toggle mode active");

                if (Sim.raycastEventManager == null)
                {
                    Debug.LogError("No raycast event manager on simualtion");
                    return;
                }
                if (GameManager.instance.Synapse == null)
                {
                    Debug.LogError("No Synapse attached to GameManager");
                    return;
                }
                //if (GameManager.instance.Synapse.GetComponent<vertexSnap>().hitEvent == null)
                //{
                //    Debug.LogError("No hit event on Synapse");
                //    return;
                //}

                GameManager.instance.Synapse.SetActive(true);
                Sim.raycastEventManager.LRTrigger = GameManager.instance.Synapse.GetComponent<vertexSnap>().hitEvent;
            }
            else if (!toggle)
            {
                GameManager.instance.Synapse.SetActive(false);
            }
            //// Enable/Disable group clamp controllers
            //if (GameManager.instance.vrDeviceManager.VRActive)
            //{
            //    if (GameManager.instance.ndClampManager.clampControllerL != null)
            //    {
            //        GameManager.instance.ndClampManager.clampControllerL.SetActive(toggle);
            //    }
            //    if (GameManager.instance.ndClampManager.clampControllerR != null)
            //    {
            //        GameManager.instance.ndClampManager.clampControllerR.SetActive(toggle);
            //    }
            //}
        }
    }
}