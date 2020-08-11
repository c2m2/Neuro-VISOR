using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction {
    /// <summary>
    /// Monitors for instances when the instantiator handle comes within range of an existing NeuronClamp
    /// </summary>
    public class NeuronClampAnchor : MonoBehaviour
    {
        public OVRInput.Button removeButton = OVRInput.Button.Two;
        public OVRInput.Button toggleButton = OVRInput.Button.Three;

        private NeuronClampInstantiator instantiator = null;
        public NeuronClamp curClamp = null;
        private bool validTrigger = false;

        private void Awake()
        {
            instantiator = GetComponentInParent<NeuronClampInstantiator>();
            if (instantiator == null) Destroy(this);
        }
        private void OnTriggerEnter(Collider other)
        {

            // Focus on clamps that aren't attached to the instantiator
            NeuronClamp clamp = other.GetComponent<NeuronClamp>();
            if (clamp == null) return;

            // If we found the current active clamp, we can't interact with existing clamps
            if (instantiator.curClamp == clamp) return;

            curClamp = clamp;
            validTrigger = true;
            // Puase instantiator input listening
            instantiator.InputOn = false;
        }
        private void OnTriggerStay(Collider other)
        {
            if (!validTrigger) return;

            // Listen for input here
            if (!instantiator.InputOn)
            {
                ListenForToggle();
                ListenForCatch();
            }
        }
        private void ListenForToggle()
        {
            if (instantiator.ToggleRequested)
            {
                curClamp.ToggleClamp();
            }
        }
        private void ListenForCatch()
        {
            if (instantiator.CreateDestroyRequested)
            {
                if (instantiator.curClamp != null) return;

                if (curClamp.clampLive) curClamp.DeactivateClamp();
                curClamp.transform.parent = transform;
                curClamp.transform.localPosition = Vector3.zero;
                curClamp.name = "UnattachedNeuronClamp";

                instantiator.curClamp = curClamp;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (!validTrigger) return;

            curClamp = null;
            validTrigger = false;
            // Resume instantiator input listening
            instantiator.InputOn = true;
        }
    }
}