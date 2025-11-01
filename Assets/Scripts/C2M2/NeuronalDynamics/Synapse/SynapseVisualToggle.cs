using UnityEngine;
using TMPro;
using C2M2.NeuronalDynamics.Simulation;
namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class SynapseVisualToggle : NDFeatureToggle
    {
        public TextMeshProUGUI buttonLabel;

        public override void OnToggle(RaycastHit hit, bool toggled)
        {
            if (toggled)
            {
                ArrowUpdate.SetGlobalMode(ArrowUpdate.VisualMode.Disk);
                buttonLabel.text = "Disk";
            }
            else
            {
                ArrowUpdate.SetGlobalMode(ArrowUpdate.VisualMode.Arrow);
                buttonLabel.text = "Arrow";
            }
        }
    }

}
