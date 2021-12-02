using System.Collections.Generic;
using UnityEngine;

public class SynapseManager : MonoBehaviour
{
    public GameObject synapse;
    bool activeSynapse = false;
    public List<Synapse> synapsesList = new List<Synapse>();
    public OVRInput.Button enableSynapse = OVRInput.Button.DpadDown;

    void Update()
    {
        #region Desktop inputs

        //If there is a current simulation and we press E then we can create a synapse
        if (Input.GetKeyDown(KeyCode.E) && synapse.GetComponent<vertexSnap>().Simulation != null && activeSynapse == false)
        {

            synapse.SetActive(true);
            activeSynapse = true;
        }

        //If we press E again set the synapse to false so we can no longer use it 
        else if(Input.GetKeyDown(KeyCode.E) && synapse.GetComponent<vertexSnap>().Simulation != null && activeSynapse == true)
        {
            synapse.SetActive(false);
            activeSynapse = false;

            // when the synapse script is turned off revert the controls back to defualt
            synapse.GetComponent<vertexSnap>().Simulation.raycastEventManager.LRTrigger = synapse.GetComponent<vertexSnap>().Simulation.defaultRaycastEvent;
        }

        #endregion

        #region oculus inputs

        // Check if the user has pressed a specified button on the right controller to activate
        else if (OVRInput.Get(enableSynapse, OVRInput.Controller.RTouch) && synapse.GetComponent<vertexSnap>().Simulation != null && activeSynapse == false)
        {
            synapse.SetActive(true);
            activeSynapse = true;
        }

        // If synapse is already activated deactivate the synapse
        else if (OVRInput.Get(enableSynapse, OVRInput.Controller.RTouch) && synapse.GetComponent<vertexSnap>().Simulation != null && activeSynapse == false)
        {
            synapse.SetActive(false);
            activeSynapse = false;

            // when the synapse script is turned off revert the controls back to defualt
            synapse.GetComponent<vertexSnap>().Simulation.raycastEventManager.LRTrigger = synapse.GetComponent<vertexSnap>().Simulation.defaultRaycastEvent;
        }

        #endregion

        //If there is no current simulation set the synapse to not active
        else if (synapse.GetComponent<vertexSnap>().Simulation == null)
        {
            synapse.SetActive(false);
            activeSynapse = false;
            
            // Remove all synapse componets attached to this object
            for(int i = 0; i < synapse.GetComponent<vertexSnap>().synapses.Count; i++)
            {
                Destroy(synapse.GetComponent<vertexSnap>().synapses[i]);
            }

            synapse.GetComponent<vertexSnap>().synapses = new List<Synapse>();
            synapsesList = synapse.GetComponent<vertexSnap>().synapses;
        }
    }
}
