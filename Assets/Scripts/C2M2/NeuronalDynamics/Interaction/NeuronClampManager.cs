using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Interaction;

namespace C2M2.NeuronalDynamics.Interaction
{
    /// <summary>
    /// Provides public method for instantiating clamps. Provides controls for multiple clamps
    /// </summary>
    public class NeuronClampManager : MonoBehaviour
    {
        public float MinPower
        {
            get
{
                return Simulation.ColorLUT.GlobalMin;
            }
        }
        public float MaxPower
        {
            get
            {
                return Simulation.ColorLUT.GlobalMax;
            }
        }
        // Sensitivity of the clamp power control. Lower sensitivity means clamp power changes more quickly
        public float sensitivity = 200f;
        public float ThumbstickScaler
        {
            get
            {
                return (MaxPower - MinPower) / sensitivity;
            }
        }

        public GameObject clampPrefab = null;
        public GameObject clampControllerR = null;
        public GameObject clampControllerL = null;
        public bool allActive = false;
        public NDSimulation Simulation
        {
            get
            {
                if (GameManager.instance.activeSim != null)
                {
                    return (NDSimulation)GameManager.instance.activeSim;
                }
                else return null;
            }
        }
        public List<NeuronClamp> Clamps {
            get
            {
                if (Simulation == null) return null;
                return Simulation.clamps;
            }
        }

        public RaycastPressEvents hitEvent = null;

        /// <summary>
        /// Looks for NDSimulation instance and adds neuronClamp object if possible
        /// </summary>
        /// <param name="hit"></param>
        public void InstantiateClamp(RaycastHit hit)
        {
            // Make sure we have a valid prefab
            if (clampPrefab == null) Debug.LogError("No Clamp prefab found");

            // If there is no NDSimulation, don't try instantiating a clamp
            if (hit.collider.GetComponentInParent<NDSimulation>() == null) return;

            int clampIndex = GetNearestPoint(hit);
            //ensures the vertex is available
            if (VertIsAvailable(clampIndex))
            {
                NeuronClamp clamp = Instantiate(clampPrefab, Simulation.transform).GetComponentInChildren<NeuronClamp>();

                clamp.ReportSimulation(Simulation, clampIndex);
            }
            
        }

        private int GetNearestPoint(RaycastHit hit)
        {
            // Translate contact point to local space
            MeshFilter mf = Simulation.transform.GetComponentInParent<MeshFilter>();
            if (mf == null) return -1;

            // Get 3D mesh vertices from hit triangle
            int triInd = hit.triangleIndex * 3;
            int v1 = mf.mesh.triangles[triInd];
            int v2 = mf.mesh.triangles[triInd + 1];
            int v3 = mf.mesh.triangles[triInd + 2];

            // Find 1D verts belonging to these 3D verts
            int[] verts1D = new int[]
            {
                Simulation.Map[v1].v1, Simulation.Map[v1].v2,
                Simulation.Map[v2].v1, Simulation.Map[v2].v2,
                Simulation.Map[v3].v1, Simulation.Map[v3].v2
            };
            Vector3 localHitPoint = Simulation.transform.InverseTransformPoint(hit.point);

            float nearestDist = float.PositiveInfinity;
            int nearestVert1D = -1;
            foreach (int vert in verts1D)
            {
                float dist = Vector3.Distance(localHitPoint, Simulation.Verts1D[vert]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVert1D = vert;
                }
            }

            return nearestVert1D;
        }

        private bool VertIsAvailable(int clampIndex)
        {
            // minimum distance between clamps 
            float distanceBetweenClamps = Simulation.AverageDendriteRadius * 2;

            foreach (NeuronClamp clamp in Simulation.clamps)
            {
                // If there is a clamp on that 1D vertex, the spot is not open
                if (clamp.FocusVert == clampIndex)
                {
                    Debug.LogWarning("Clamp already exists on focus vert [" + clampIndex + "]");
                    return false;
                }
                // If there is a clamp within distanceBetweenClamps, the spot is not open
                else
                {

                    float dist = (Simulation.Verts1D[clamp.FocusVert] - Simulation.Verts1D[clampIndex]).magnitude;
                    if (dist < distanceBetweenClamps)
                    {
                        Debug.LogWarning("Clamp too close to clamp located on vert [" + clamp.FocusVert + "].");
                        return false;
                    }
                }
            }
            return true;
        }

        private int destroyCount = 50;
        private int holdCount = 0;
        /// <summary>
        /// Pressing these buttonb toggles clamps on/off. Holding these buttons down for long enough destroys the clamp
        /// </summary>
        public OVRInput.Button toggleDestroyOVR = OVRInput.Button.PrimaryIndexTrigger;
        public OVRInput.Button toggleDestroyOVRS = OVRInput.Button.SecondaryIndexTrigger;
        public bool PressedToggleDestroy
        {
            get
            {
                if (GameManager.instance.VrIsActive)
                    return (OVRInput.Get(toggleDestroyOVR) || OVRInput.Get(toggleDestroyOVRS));
                else return true;
            }
        }
        public KeyCode powerModifierPlusKey = KeyCode.UpArrow;
        public KeyCode powerModifierMinusKey = KeyCode.DownArrow;
        public float PowerModifier
        {
            get
            {
                if (GameManager.instance.VrIsActive)
                {
                    // Uses the value of both joysticks added together
                    float y1 = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                    float y2 = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                    float scaler = y1 + y2;

                    return ThumbstickScaler * scaler;
                }
                else
                {
                    if (Input.GetKey(powerModifierPlusKey)) return ThumbstickScaler;
                    if (Input.GetKey(powerModifierMinusKey)) return -ThumbstickScaler;
                    else return 0;
                }
            }
        }
        private bool powerClick = false;

        public OVRInput.Button highlightOVR = OVRInput.Button.PrimaryHandTrigger;
        public OVRInput.Button highlightOVRS = OVRInput.Button.SecondaryHandTrigger;
        public bool PressedHighlight
        {
            get
            {
                if (GameManager.instance.VrIsActive)
                    return (OVRInput.Get(highlightOVR) || OVRInput.Get(highlightOVRS));
                else return false; // We cannot highlight through the emulator
            }
        }

        public OVRInput.Button cancelCommand = OVRInput.Button.Two;
        public OVRInput.Button cancelCommandS = OVRInput.Button.Four;
        public KeyCode cancelKey = KeyCode.Backspace;
        public bool PressedCancel
        {
            get
            {
                if (GameManager.instance.VrIsActive)
                    return (OVRInput.Get(cancelCommand) || OVRInput.Get(cancelCommandS));
                else
                    return Input.GetKey(cancelKey);
            }
        }
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void MonitorInput()
        {
            if (PressedToggleDestroy)
                holdCount++;
            else
                CheckInput();

            // Highlight all clamps if either hand trigger is held down
            HighlightAll(PressedHighlight);

            float power = PowerModifier;
            // If clamp power is modified while the user holds a click, don't let the click also toggle/destroy the clamp
            if (power != 0 && !powerClick) powerClick = true;

            foreach (NeuronClamp clamp in Clamps)
            {
                if (clamp != null) clamp.ClampPower += power;
            }       
        }

        public void ResetInput()
        {
            CheckInput();
            HighlightAll(false);
        }

        private void CheckInput()
        {
            if (!powerClick)
            {
                if (holdCount >= destroyCount)
                    DestroyAll();
                else if (holdCount > 0)
                    ToggleAll();
            }

            holdCount = 0;
            powerClick = false;
        }

        private void ToggleAll()
        {
            if (Clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in Clamps)
                {
                    if (clamp != null && clamp.FocusVert != -1) {
                        if (allActive)
                            clamp.DeactivateClamp();
                        else
                            clamp.ActivateClamp();
                    }
                }

                allActive = !allActive;
            }
        }
        private void DestroyAll()
        {
            if (Clamps.Count > 0)
            {
                Simulation.clampMutex.WaitOne();
                foreach (NeuronClamp clamp in Clamps)
                {
                    if (clamp != null && clamp.FocusVert != -1)
                        Destroy(clamp.transform.parent.gameObject);
                }
                Clamps.Clear();
                Simulation.clampMutex.ReleaseMutex();
            }
        }
        public void HighlightAll(bool highlight)
        {
            if (Clamps.Count > 0)
            {
                foreach (NeuronClamp clamp in Clamps)
                {
                    clamp.Highlight(highlight);
                }
            }
        }
    }
}