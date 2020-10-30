using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace C2M2
{
    using Interaction;
    using Interaction.UI;
    using Interaction.VR;
    using NeuronalDynamics.Interaction;
    /// <summary>
    /// Stores many global variables, handles pregame initializations
    /// </summary>
    [RequireComponent(typeof(NeuronClampInstantiator))]
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance = null;
        
        public int mainThreadId { get; private set; } = -1;
        public string assetsPath { get; private set; } = null;

        public VRDeviceManager vrDeviceManager = null;
        public bool vrIsActive {
            get
            {
                if (vrDeviceManager == null) Debug.LogError("No VR Device Manager Found!");
                return vrDeviceManager.vrIsActive;
            }
        }

        public NeuronClampInstantiator clampInstantiator = null;
        public GameObject[] clampControllers = new GameObject[0];

        [Header("Materials")]
        public Material defaultMaterial;
        public Material vertexColorationMaterial;
        public Material lineRendMaterial;

        [Tooltip("Used as an anchor point for neuron diameter control panel")]
        public Transform whiteboard = null;
        public Vector3 objScaleDefault = new Vector3(2f, 2f, 2f);
        public Vector3 objScaleMax = new Vector3(4f, 4f, 4f);
        public Vector3 objScaleMin = new Vector3(0.3f, 0.3f, 0.3f);

        [Header("OVR Player Controller")]
        public GameObject ovrRightHandAnchor = null;
        public GameObject ovrLeftHandAnchor = null;
        public OVRPlayerController ovrPlayerController { get; set; } = null;
        public GameObject nonVRCamera { get; set; } = null;

        [Header("FPS Counter")]
        public Utils.DebugUtils.FPSCounter fpsCounter;
        private bool isRunning = false;

        [Header("Obsolete")]
        public RaycastForward rightRaycaster;
        public RaycastForward leftRaycaster;
        public GameObject menu = null;
        public GameObject raycastKeyboardPrefab;
        public RaycastKeyboard raycastKeyboard { get; set; }
        public Transform menuSnapPosition;

        private void Awake()
        {
            assetsPath = Application.dataPath;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            // Initialize the GameManager
            DontDestroyOnLoad(gameObject);
            if (instance == null) { instance = this; }
            else if (instance != this) { Destroy(this); }

            clampInstantiator = GetComponent<NeuronClampInstantiator>();
            // Initialize keyboard
            //raycastKeyboardPrefab = Instantiate(raycastKeyboardPrefab, new Vector3(50, 50, 50), Quaternion.identity);
            //raycastKeyboard = raycastKeyboardPrefab.GetComponent<RaycastKeyboard>();
            isRunning = true;

        }

        private void Update()
        {
            if(logQ != null && logQ.Count > 0)
            { // print every queued statement
                foreach (string s in logQ) { Debug.Log(s); }
                logQ.Clear();
            }
            if (eLogQ != null && eLogQ.Count > 0)
            { // print every queued statement
                foreach (string s in eLogQ) { Debug.LogError(s); }
                eLogQ.Clear();
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
            if (isRunning)
            {
                if (logQ.Count > logQCap)
                {
                    Debug.LogWarning("Cannot call DebugLogSafe more than [" + logQCap + "] times per frame. New statements will not be added to queue");
                    return;
                }
                logQ.Add(s);
            }
        }
        public void DebugLogThreadSafe<T>(T t) => DebugLogSafe(t.ToString());

        private List<string> eLogQ = new List<string>();
        private readonly int eLogQCap = 100;
        /// <summary>
        /// Allows other threads to submit messages to be printed at the start of the next frame
        /// </summary>
        /// <remarks>
        /// Making any Unity API call from another thread is not safe. This method is a quick hack
        /// to avoid mkaing a Unity API call from another thread.
        /// </remarks>
        public void DebugLogErrorSafe(string s)
        {
            if (isRunning)
            {
                if (eLogQ.Count > eLogQCap)
                {
                    Debug.LogWarning("Cannot call DebugLogSafe more than [" + logQCap + "] times per frame. New statements will not be added to queue");
                    return;
                }
                eLogQ.Add(s);
            }
        }
        public void DebugLogErrorThreadSafe<T>(T t) => DebugLogErrorSafe(t.ToString());

        private void OnApplicationQuit()
        {
            isRunning = false;
        }
        private void OnApplicationPause(bool pause)
        {
            isRunning = !pause;
        }
    }
}