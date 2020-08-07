using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Interaction
{
    
    public class NeuronClampInstantiator : MonoBehaviour
    {
        public GameObject ClampPrefab = null;
        private NeuronClamp curClamp = null;
        public OVRInput.Button button = OVRInput.Button.One;
        public List<NeuronClamp> allClamps = new List<NeuronClamp>();
        public Transform clampAnchor = null;
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
            if(curClamp.transform.parent != transform)
            {
                allClamps.Add(curClamp);
                curClamp = null;
                InstantiateClamp();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Toggling all clamps");
                foreach(NeuronClamp clamp in allClamps)
                {
                    if(clamp != null)
                    {
                        clamp.ToggleClamp();
                    }
                }
            }
        }

        private void InstantiateClamp()
        {
            if (curClamp == null)
            {
                curClamp = Instantiate(ClampPrefab, clampAnchor ?? transform).GetComponent<NeuronClamp>();
                curClamp.transform.localPosition = Vector3.zero;
            }
        }
    }
}