#pragma warning disable 0618 // Ignore obsolete script warning

using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace C2M2
{
    using Interaction.VR;
    using NeuronalDynamics.Interaction;
    using NeuronalDynamics.Interaction.UI;
    using NeuronalDynamics.Simulation;
    using Simulation;

    /// <summary>
    /// Stores many global variables, handles pregame initializations
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance = null;

        public static int simID = 0; // current simulation ID

        // vectors used in SparseSolve; for loading
        public double[] U;
        public double[] M;
        public double[] N;
        public double[] H;

        public double[] Upre;
        public double[] Mpre;
        public double[] Npre;
        public double[] Hpre;

        // for loading a file
        private bool loading = false;
        public bool Loading
        {
            get { return loading; }
            set { loading = value; }
        }

        public int mainThreadId { get; private set; } = -1;

        public VRDeviceManager vrDeviceManager = null;
        public bool VRActive { get { return vrDeviceManager.VRActive; } }

        public GameObject cellPreviewer = null;
        /// <summary>
        /// Default gradient for coloring the mesh's surface
        /// </summary>
        public Gradient defaultGradient;
        public List<Interactable> activeSims = new List<Interactable>();
        public readonly object activeSimsLock = new object();
        public GameObject clampManagerPrefab = null;
        public GameObject clampManagerL = null;
        public GameObject clampManagerR = null;

        public GameObject synapseManagerPrefab = null;

        public GameObject graphManagerPrefab = null;

        /// <summary>
        /// Allows solver threads to be synched
        /// </summary>
        public Barrier solveBarrier = new Barrier(0);

        public NDSimulationManager simulationManager = null;
        public GameObject simulationSpace = null;

        [Header("Environment")]
        public int roomSelected = 0;
        public Room[] roomOptions = null;
        public Color wallColor = Color.white;
        [Header("Materials")]
        public Material vertexColorationMaterial = null;
        public Material lineRendMaterial = null;

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

        private void Awake()
        {
            // Initialize the GameManager
            DontDestroyOnLoad(gameObject);
            if (instance == null) { instance = this; }
            else if (instance != this) { Destroy(this); }

            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            if(roomOptions != null && roomOptions.Length > 0)
            {
                roomSelected = Mathf.Clamp(roomSelected, 0, roomOptions.Length - 1);
                // Only enable selected room, disable all others
                for(int i = 0; i < roomOptions.Length; i++)
                {
                    roomOptions[i].gameObject.SetActive(i == roomSelected);
                }
                // Apply wall color to selected room's walls
                if (roomOptions[roomSelected].walls != null && roomOptions[roomSelected].walls.Length > 0)
                {
                    foreach (MeshRenderer wall in roomOptions[roomSelected].walls)
                    {
                        if (wall != null)
                        {
                            wall.material.color = wallColor;
                        }
                        else
                        {
                            Debug.LogWarning("Wall's meshrenderer was null on " + roomOptions[roomSelected].name);
                        }
                    }
                }
            }

            if (cellPreviewer != null)
            {
                cellPreviewer = GameObject.Instantiate(cellPreviewer);
            }
            else
            {
                Debug.LogError("No cell previewer prefab given!");
            }

            isRunning = true;
        }

        private void Update()
        {
            string msg;
            while (logQ.TryDequeue(out msg)) { Debug.Log(msg); }
            while (eLogQ.TryDequeue(out msg)) { Debug.LogError(msg); }
        }

        private readonly ConcurrentQueue<string> logQ = new ConcurrentQueue<string>();
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
                    return;
                }
                logQ.Enqueue(s);
            }
        }
        public void DebugLogThreadSafe<T>(T t) => DebugLogSafe(t.ToString());

        private readonly ConcurrentQueue<string> eLogQ = new ConcurrentQueue<string>();
        private readonly int eLogQCap = 100;
        /// <summary>
        /// Allows other threads to submit messages to be printed at the start of the next frame
        /// </summary>
        /// <remarks>
        /// Making any Unity API call from another thread is not safe. This method is a quick hack
        /// to avoid making a Unity API call from another thread.
        /// </remarks>
        public void DebugLogErrorSafe(string s)
        {
            if (isRunning)
            {
                if (eLogQ.Count > eLogQCap)
                {
                    return;
                }
                eLogQ.Enqueue(s);
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
