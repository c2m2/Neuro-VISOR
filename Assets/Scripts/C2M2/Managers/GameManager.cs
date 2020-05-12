using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using GetSocialSdk.Capture.Scripts;
namespace C2M2
{
    [RequireComponent(typeof(VirtualRealityEnabled))]
    public class GameManager : MonoBehaviour
    {
        public GetSocialCapture screenRecorder;
        public int mainThreadId = -1;
        public string assetsPath = null;
        [HideInInspector]
        public bool useVirtualReality;

        public static GameManager instance = null;
        [Header("Environment")]
        public Material defaultMaterial;
        public Material vertexColorationMaterial;

        public Vector3 objectScaleDefault = new Vector3(1f, 1f, 1f);
        //[Header("Raycasters")]
        public RaycastForward rightRaycaster;
        public RaycastForward leftRaycaster;
        [Header("OVR Player Controller")]
        public GameObject ovrRightHandAnchor = null;
        public GameObject ovrLeftHandAnchor = null;
        public OVRPlayerController ovrPlayerController { get; set; } = null;
        public GameObject nonVRCamera { get; set; } = null;
        [Header("Menu")]
        public GameObject menu = null;
        [Header("Raycast Keyboard")]
        public GameObject raycastKeyboardPrefab;
        public RaycastKeyboard raycastKeyboard { get; set; }
        public Transform menuSnapPosition;
        [Header("FPS Counter")]
        public Utilities.Debugging.FPSCounter fpsCounter;


        private void Awake()
        {
            assetsPath = Application.dataPath;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            // Initialize the GameManager
            DontDestroyOnLoad(gameObject);
            if (instance == null) { instance = this; }
            else if (instance != this) { Destroy(this); }
            // Initialize keyboard
            raycastKeyboardPrefab = Instantiate(raycastKeyboardPrefab, new Vector3(50, 50, 50), Quaternion.identity);
            raycastKeyboard = raycastKeyboardPrefab.GetComponent<RaycastKeyboard>();
            // Make sure VREnabled and GameManager have the same OVRPlayerController/NonVRCamera
            VirtualRealityEnabled vrEnabled = GetComponent<VirtualRealityEnabled>();
            // Make sure this vrEnabled and this have the same non-VR camera
            if (vrEnabled.nonVRCamera != null) { nonVRCamera = vrEnabled.nonVRCamera; }
            else if (nonVRCamera != null) { vrEnabled.nonVRCamera = nonVRCamera; }
            // Make sure this vrEnabled and this have the same OVRPlayerController
            if (vrEnabled.ovrPlayerController != null) { ovrPlayerController = vrEnabled.ovrPlayerController; }
            else if (ovrPlayerController != null) { vrEnabled.ovrPlayerController = ovrPlayerController; }

            screenRecorder = Camera.main.gameObject.GetComponent<GetSocialCapture>();

        }

        private void Update()
        {
            if(logQ != null && logQ.Count > 0)
            { // print every queued statement
                foreach (string s in logQ) { Debug.Log(s); }
                logQ.Clear();
            }
        }
        public void RaycasterRightChangeColor(Color color) => rightRaycaster.ChangeStaticHandColor(color);
        public void RaycasterLeftChangeColor(Color color) => leftRaycaster.ChangeStaticHandColor(color);


        private List<string> logQ = new List<string>();
        private readonly int logQCap = 100;
        /// <summary>
        /// Allows other threads to submit messages to be printed at the start of the next frame
        /// </summary>
        /// <remarks>
        /// Making any Unity API call from another thread is not safe. This method is a quick hack
        /// to avoid mkaing a Unity API call from another thread.
        /// </remarks>
        public void DebugLogSafe(string s)
        {
            if(logQ.Count > logQCap)
            {
                Debug.LogWarning("Cannot call DebugLogSafe more than [" + logQCap + "] times per frame. New statements will not be added to queue");
                return;
            }
            logQ.Add(s);
        }
        public void DebugLogSafe<T>(T t) => DebugLogSafe(t.ToString());
    }
}