using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace C2M2.Utils
{
    /// <summary>
    /// Only allow this MeshRenderer (and collider) to be enabled if a parent's is
    /// </summary>
    /// <remarks>
    /// It is useful to allow this gameobject to remain enabled while not allowing it to be interactable.
    /// </remarks>
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshRenderChild : MonoBehaviour
    {
        // If disabled, children are always set to off
        public MeshRenderer parent = null;
        public Collider optionalCollider = null;
        private MeshRenderer mr = null;

        private void Awake()
        {
            if(parent == null)
            {
                parent = GetComponent<MeshRenderer>();
                if(parent == null)
                    Debug.LogError("No MeshRenderer given to MeshRenderChild");
            }
            mr = GetComponent<MeshRenderer>();
           
        }

        // Update is called once per frame
        void Update()
        {
            mr.enabled = parent.enabled;
            if (optionalCollider != null)
            {
                optionalCollider.enabled = parent.enabled;
            }
        }
    }
}