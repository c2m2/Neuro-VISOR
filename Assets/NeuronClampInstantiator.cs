﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Instantiates NeuronClamps, keeps track of them, 
    /// </summary>
    public class NeuronClampInstantiator : MonoBehaviour
    {
        public GameObject ClampPrefab = null;
        public NeuronClamp curClamp = null;
        public List<NeuronClamp> allClamps = new List<NeuronClamp>();
        public Transform clampAnchor = null;

        [Header("Input")]
        private bool inputOn = true;
        public bool InputOn { get; set; }
        public OVRInput.Button createDestroyButton = OVRInput.Button.One;
        private KeyCode createDestroyKey = KeyCode.N;
        public bool CreateDestroyRequested
        {
            get
            { 
                // In VR, only allow controls if the handle is grabbed
                return ((grabbable.isGrabbed && OVRInput.GetDown(createDestroyButton)) 
                    || Input.GetKeyDown(createDestroyKey));
            }
        }
        public OVRInput.Button toggleClampsButton = OVRInput.Button.Two;
        private KeyCode toggleKey = KeyCode.Space;
        public bool ToggleRequested
        {
            get
            {
                return (grabbable.isGrabbed && OVRInput.GetDown(toggleClampsButton)) 
                    || Input.GetKeyDown(toggleKey);
            }
        }
        private static Vector3 defaultLocalScale = new Vector3(2.5f, 0.25f, 2.5f);
        private OVRGrabbable grabbable = null;

        private bool clampsActivated = false;

        private void Awake()
        {
            if (ClampPrefab == null)
            {
                Debug.LogError("No clamp prefab given.");
                Destroy(this);
            }
            InstantiateClamp();
            defaultLocalScale = curClamp.transform.localScale;
            grabbable = GetComponent<OVRGrabbable>();
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.tag == "NeuronClamp")
            {
                NeuronClamp clamp = other.GetComponent<NeuronClamp>();
                if (clamp != null)
                {
                    allClamps.Add(clamp);
                    clamp.name = "UncampedNeuronClamp";
                    curClamp = null;
                    InstantiateClamp();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            /*
            // If our clamp has latched onto a simulation, add another clamp
            if (curClamp != null)
            {

                if (curClamp != null && curClamp.transform.hasChanged)
                {
                    curClamp.transform.localPosition = Vector3.zero;
                    curClamp.transform.localRotation = Quaternion.identity;
                    curClamp.transform.localScale = defaultLocalScale;
                    curClamp.transform.hasChanged = false;
                }
            }*/

            // Instantiate a new clamp if requested
            if (InputOn)
            {
                ListenForClampToggle();
            }
        }

        private void ListenForClampToggle()
        {
            // Toggle clamps if requested
            if (allClamps.Count > 0 && ToggleRequested)
            {
                // If "all clamps" state is on, deactivate all clamps
                if (clampsActivated)
                {
                    foreach (NeuronClamp clamp in allClamps)
                    {
                        if (clamp != null)
                        {
                            clamp.DeactivateClamp();
                        }
                    }
                }
                else
                {
                    foreach (NeuronClamp clamp in allClamps)
                    {
                        if (clamp != null)
                        {
                            clamp.ActivateClamp();
                        }
                    }
                }
                clampsActivated = !clampsActivated;
            }
        }

        public void InstantiateClamp()
        {
            if (curClamp == null)
            {
                curClamp = Instantiate(ClampPrefab, transform).GetComponent<NeuronClamp>();
                curClamp.transform.localPosition = Vector3.zero;
                curClamp.name = "CampedNeuronClamp";
            }
        }
        public void DestroyClamp(NeuronClamp clamp)
        {
            allClamps.Remove(clamp);
            Destroy(clamp.gameObject);
            curClamp = null;
        }
    }
}