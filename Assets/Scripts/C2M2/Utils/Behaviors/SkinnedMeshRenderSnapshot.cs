﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR;

namespace C2M2.Utils.DebugUtils
{
#if (UNITY_EDITOR)
    /// <summary>
    /// Press a button to save a snapshot of a skinned mesh renderer. Attach this script to an object with a skinnedMeshRenderer before or during runtime.
    /// </summary>
    public class SkinnedMeshRenderSnapshot : MonoBehaviour
    {
        public SecondaryButtonEvent secondaryButtonPress = new SecondaryButtonEvent();

        [Tooltip("Location to save mesh to")]
        public string saveLocation = "Assets/";

        [Tooltip("Button to press to take a snapshot")]
        public KeyCode key = KeyCode.Space;
        
        [Tooltip("Number of meshes already stored. Change this manually if you need to ensure that meshes won't overwrite eachother")]
        public int count = 0;

        [Tooltip("Save skinnedMeshRenderer.mesh immediately?")]
        public bool retrieveImmediate = true;

        private SkinnedMeshRenderer smr;

        private bool lastSecondaryButtonState = false;
        private List<InputDevice> handControllers = new List<InputDevice>();

        private void Awake()
        {
            InputDeviceCharacteristics desiredCharacteristics = InputDeviceCharacteristics.Left;
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, handControllers);
        }

        void Start()
        {
            smr = GetComponent<SkinnedMeshRenderer>();

            //If there is no skinned mesh renderer, destory yourself
            if (smr == null)
            {
                Debug.Log("NO SKINNED MESH RENDERER FOUND");
                Destroy(this);
            }

            if (retrieveImmediate)
            {
                Mesh meshSnapshot = new Mesh();
                smr.BakeMesh(meshSnapshot);
                AssetDatabase.CreateAsset(meshSnapshot, saveLocation + "mesh" + count.ToString() + ".asset");
                AssetDatabase.SaveAssets();
                count++;
            }
        }

        void Update()
        {
            bool tempState = false;
            foreach (var device in handControllers)
            {
                bool secondaryButtonState = false;
                tempState = device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButtonState) // did get a value
                            && secondaryButtonState // the value we got
                            || tempState; // cumulative result from other controllers
            }
            bool isPressed = tempState != lastSecondaryButtonState;
            if (isPressed) // Button state changed since last frame
            {
                secondaryButtonPress.Invoke(tempState);
                lastSecondaryButtonState = tempState;
            }
            if (isPressed)
            {
                Debug.Log("Saving snapshot...");
                //Save the mesh
                Mesh meshSnapshot = new Mesh();
                smr.BakeMesh(meshSnapshot);

                //Create and save mesh assets to desired location with (hopefully) unique name
                AssetDatabase.CreateAsset(meshSnapshot, saveLocation + "mesh" + count.ToString() + ".asset");
                AssetDatabase.SaveAssets();

                //Avoid mesh overwrites
                count++;

                // Print the path of the created asset
                Debug.Log("Snapshot saved to " + AssetDatabase.GetAssetPath(meshSnapshot));
            }
        }
    }
#endif
}