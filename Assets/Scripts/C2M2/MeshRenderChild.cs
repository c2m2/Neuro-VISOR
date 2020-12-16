using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace C2M2.Utils
{
    /// <summary>
    /// If a given MeshRenderer is disabled, this gameobject will disable.
    /// </summary>
    /// <remarks>
    /// This is useful in the situation where we have the static pointing finger
    /// for raycasting mode, and we have a feature that relies on being in raycasting mode.
    /// This way, when the static finger model is disabled, this object and its scripts/colliders will also disable
    /// </remarks>
    [ExecuteInEditMode]
    public class MeshRenderChild : MonoBehaviour
    {
        public MeshRenderer parent = null;
        public GameObject[] children = new GameObject[0];

        private void Awake()
        {
            if(parent == null)
            {
                parent = GetComponent<MeshRenderer>();
                if(parent == null)
                    Debug.LogError("No MeshRenderer given to MeshRenderChild");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (parent.enabled) Toggle(true);
            else Toggle(false);
        }

        private void Toggle(bool toggleTo)
        {
            if (children.Length > 0)
            {
                foreach (GameObject child in children) child.SetActive(toggleTo);
            }else if (Application.isPlaying)
            {
                Destroy(this);
            }

        }
    }
}