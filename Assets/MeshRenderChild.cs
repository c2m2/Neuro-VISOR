using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public MonoBehaviour[] monosToDisable = null;
        public Collider[] colsToDisable = null;

        private void Awake()
        {
            if(parent == null)
            {
                Debug.LogError("No MeshRenderer given to MeshRenderChild");
            }
            if(monosToDisable.Length == 0)
            {
                Debug.LogWarning("No components given to MeshRenderChild to disable/enable");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (parent.enabled) Toggle(false);
            else Toggle(true);
        }

        private void Toggle(bool toggleTo)
        {
            foreach (MonoBehaviour comp in monosToDisable) comp.enabled = toggleTo;

        }
    }
}