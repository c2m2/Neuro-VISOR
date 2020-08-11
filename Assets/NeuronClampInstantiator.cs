using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Instantiates NeuronClamps, keeps track of them, 
    /// </summary>
    [RequireComponent(typeof(OVRGrabbable))]
    public class NeuronClampInstantiator : MonoBehaviour
    {
        public GameObject ClampPrefab = null;
        public NeuronClamp curClamp = null;
        public List<NeuronClamp> allClamps = new List<NeuronClamp>();
        public Transform clampAnchor = null;

        [Header("Input")]
        private bool inputOn = true;
        public bool InputOn { get; set; }
        public OVRInput.Button createDestroyButton = OVRInput.Button.Two;
        private KeyCode createDestroyKey = KeyCode.N;
        public bool CreateDestroyRequested
        {
            get
            {
                return (OVRInput.GetDown(createDestroyButton) || Input.GetKeyDown(createDestroyKey));
            }
        }
        public OVRInput.Button toggleClampsButton = OVRInput.Button.Three;
        private KeyCode toggleKey = KeyCode.Space;
        public bool ToggleRequested
        {
            get
            {
                return OVRInput.GetDown(toggleClampsButton) || Input.GetKeyDown(toggleKey);
            }
        }

        private void Awake()
        {
            if (ClampPrefab == null || clampAnchor == null)
            {
                Debug.LogError("No clamp prefab given.");
                Destroy(this);
            }
            InstantiateClamp();
        }

        // Update is called once per frame
        void Update()
        {
            // If our clamp has latched onto a simulation, add another clamp
            if (curClamp != null)
            {
                if (curClamp.transform.parent == null || curClamp.transform.parent != clampAnchor)
                {
                    curClamp = null;
                }
            }

            // Instantiate a new clamp if requested
            if (InputOn)
            {
                ListenForClampCreation();
                ListenForClampToggle();
            }
        }

        private void ListenForClampCreation()
        {
            if (CreateDestroyRequested)
            {
                if (curClamp == null) InstantiateClamp();
                else
                {
                    DestroyClamp(curClamp);
                }
            }
        }
        private void ListenForClampToggle()
        {
            // Toggle clamps if requested
            if (allClamps.Count > 0 && ToggleRequested)
            {
                foreach (NeuronClamp clamp in allClamps)
                {
                    if (clamp != null)
                    {
                        clamp.ToggleClamp();
                    }
                }
            }
        }

        public void InstantiateClamp()
        {
            if (curClamp == null)
            {
                curClamp = Instantiate(ClampPrefab, clampAnchor ?? transform).GetComponent<NeuronClamp>();
                curClamp.transform.localPosition = Vector3.zero;
                curClamp.name = "UnattachedNeuronClamp";
                allClamps.Add(curClamp);
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