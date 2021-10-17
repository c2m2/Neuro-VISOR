using C2M2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynapseManager : MonoBehaviour
{
    public GameObject synapse;
    bool activeSynapse = false;

    void Update()
    {
        //If there is a current simulation and we press E then we can create a synapse
        if (Input.GetKeyDown(KeyCode.E) && GameManager.instance.activeSim != null && activeSynapse == false)
        {
            synapse.SetActive(true);
            activeSynapse = true;
        }

        //If we press E again set the synapse to false so we can no longer use it 
        else if(Input.GetKeyDown(KeyCode.E) && GameManager.instance.activeSim != null && activeSynapse == true)
        {
            synapse.SetActive(false);
            activeSynapse = false;
        }

        //If there is no current simulation set the synapse to not active
        if(GameManager.instance.activeSim == null)
        {
            synapse.SetActive(false);
            activeSynapse = false;
        }
    }
}
