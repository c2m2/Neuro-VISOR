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

                GameManager.instance.Synapse.SetActive(true);
                Sim.raycastEventManager.LRTrigger = GameManager.instance.Synapse.GetComponent<SynapseManager>().hitEvent;
            }
            else if (!toggle)
            {
                GameManager.instance.Synapse.SetActive(false);
            }
        }
    }
}