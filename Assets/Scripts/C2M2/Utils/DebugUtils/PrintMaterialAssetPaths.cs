using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace C2M2.Utils.DebugUtils
{
#if (UNITY_EDITOR)
    public class PrintMaterialAssetPaths : MonoBehaviour
    {
        public Material mat;

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                Debug.Log("Spacebar was pressed");
                Debug.Log("Name: " + AssetDatabase.GetAssetPath(mat));
                Debug.Log("Path: " + AssetDatabase.GetAssetPath(mat));
            }
        }
    }
#endif
}