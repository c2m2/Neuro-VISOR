using UnityEngine;
using C2M2.Utils;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System;
using C2M2.NeuronalDynamics.Interaction;
using System.Collections.Generic;
using System.Linq;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.Interaction
{
    /// <summary>
    /// Controls the scaling of a transform
    /// </summary>
    [RequireComponent(typeof(OVRGrabbable))]
    public class GrabRescaler : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;
        private MeshRenderer meshrender = null;
        private SphereCollider pivotcollider = null;
        private Vector3 origScale;
        private Vector3 minScale;
        private Vector3 maxScale;
        public float scaler = 50f;
        public float scaleRate = 0.04f;
        public float minPercentage = 0.1f;
        public float maxPercentage = 5f;
        public bool xScale = true;
        public bool yScale = true;
        public bool zScale = true;
        public OVRInput.RawButton LeftX = OVRInput.RawButton.X;
        public KeyCode incKey = KeyCode.UpArrow;
        public KeyCode decKey = KeyCode.DownArrow;
        public KeyCode visibilityToggleKey = KeyCode.R;
        public Transform target = null;
        public Vector3 median = Vector3.zero;
        public List<Vector3> neuronPositions = new List<Vector3>(new Vector3[1]);
        public int lastNeuronCount = 0;
        public GameObject[] neuronList;
        public NeuronClamp[] clampList;
        private GameObject[] pivotPoints;
        public GameObject pivot;

        // Desktop grab state for moving all neurons without VR
        private bool isDesktopGrabbed = false;
        private Vector3 desktopGrabOffset;
        private float desktopGrabScreenZ;
        public int desktopGrabMouseButton = 1; // right mouse button

        /// <summary>
        /// True when the pivot is grabbed in VR or being dragged on desktop
        /// </summary>
        private bool IsGrabbed
        {
            get { return grabbable.isGrabbed || isDesktopGrabbed; }
        }

        private float ChangeScaler
        {
            get
            {
                ///<returns>A float between -1 and 1, where -1 means the thumbstick y axis is completely down and 1 implies it is all the way up</returns>
                if (GameManager.instance.vrDeviceManager.VRActive) return (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y);
                else if (Input.GetKey(incKey) && !Input.GetKey(decKey)) return 1f;
                else if (Input.GetKey(decKey) && !Input.GetKey(incKey)) return -1f;
                return 0;
            }
        }

        ///<returns>A boolean of whether the visibility toggle key is pressed</returns>
        private bool VisibilityCheck()
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                if (OVRInput.GetDown(LeftX) && !OVRInput.Get(OVRInput.RawButton.Y))
                {
                    meshrender.enabled = !meshrender.enabled;
                    pivotcollider.enabled = !pivotcollider.enabled;
                    return true;
                }
            }
            else
            {
                if (Input.GetKeyDown(visibilityToggleKey))
                {
                    meshrender.enabled = !meshrender.enabled;
                    pivotcollider.enabled = !pivotcollider.enabled;
                    return true;
                }
            }
            return false;
        }

        // Checks whether the user is raycasting at a Clamp; disables rescaling functionality if so
        // Note: Uses the RescaleCheck variable of the Clamp
        private bool ClampCheck()
        {
            for (int i = 0; i < clampList.Count(); i++)
            {
                if (clampList[i].RescaleCheck)
                {
                    return true;
                }
            }
            return false;
        }

        private void Start()
        {
            // transform refers to the transform of the SimulationSpace object which contains all neurons
            target = GameManager.instance.simulationSpace.transform;

            // Get relevant components to toggle visibility, grabbability, and object colliders
            grabbable = GetComponent<OVRGrabbable>();
            meshrender = GetComponent<MeshRenderer>();
            pivotcollider = GetComponent<SphereCollider>();

            // Get the only NeuronPivotPoint object
            pivotPoints = GameObject.FindGameObjectsWithTag("NeuronPivotPoint");
            pivot = pivotPoints[0];

            // Set minScale and maxScale limits based on initial scale of the SimulationSpace object
            origScale = target.localScale;
            minScale = minPercentage * origScale;
            if (maxPercentage == float.PositiveInfinity) maxScale = Vector3.positiveInfinity;
            else maxScale = maxPercentage * origScale;
        }

        void Update()
        {
            // Store all loaded Neurons in the simulation into a list
            neuronList = GameObject.FindGameObjectsWithTag("SimulatedNeuronCell");
            // Check if a Neuron was added since the last frame, then toggle pivotpoint visibility accordingly
            if (neuronList.Count() - lastNeuronCount >= 1)
            {
                if (lastNeuronCount >= 1)
                {
                    meshrender.enabled = true;
                    pivotcollider.enabled = true;
                }
                else
                {
                    meshrender.enabled = false;
                    pivotcollider.enabled = false;
                }
            }
            lastNeuronCount = neuronList.Count();

            // Check the list of currently loaded Clamps to see if the user is raycasting towards a clamp
            // TODO: Create and track this separately by adding Clamps to a universal list as they're generated
            clampList = FindObjectsOfType<NeuronClamp>();

            // Check if the user is toggling the visibility of the pivotpoint object
            VisibilityCheck();

            // Desktop grab detection: right-click drag on the pivot ball to move all neurons
            if (!GameManager.instance.vrDeviceManager.VRActive)
            {
                if (Input.GetMouseButtonDown(desktopGrabMouseButton) && pivotcollider.enabled)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit) && hit.collider == pivotcollider)
                    {
                        isDesktopGrabbed = true;
                        desktopGrabScreenZ = Camera.main.WorldToScreenPoint(pivot.transform.position).z;
                        desktopGrabOffset = pivot.transform.position - Camera.main.ScreenToWorldPoint(
                            new Vector3(Input.mousePosition.x, Input.mousePosition.y, desktopGrabScreenZ));
                    }
                }
                if (Input.GetMouseButtonUp(desktopGrabMouseButton))
                {
                    isDesktopGrabbed = false;
                }
                if (isDesktopGrabbed)
                {
                    Vector3 mouseScreenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, desktopGrabScreenZ);
                    pivot.transform.position = Camera.main.ScreenToWorldPoint(mouseScreenPos) + desktopGrabOffset;
                }
            }

            // Calculate the average point between each simulated Neuron
            GeometricMedian(target);

            // If the pivotpoint object is not grabbed, snap it to the median and rescale if requested
            if (!IsGrabbed)
            {
                SetNeuronParents(target);
                pivot.transform.position = median;
                if (ChangeScaler != 0) Rescale();
            }
            // If the pivotpoint object is grabbed (VR or desktop), reparent neurons for group movement
            if (IsGrabbed)
            {
                SetNeuronParents(pivot.transform);
            }
        }

        public void Rescale()
        {
            // Use of scaleStep ensures pace of changes is consistent regardless of simulation FPS
            float scaleStep = scaler * Time.deltaTime;
            if (clampList.Count() > 0) if (ClampCheck()) scaleStep = 0f;
            // Equivalent to exponential rescaling of scale by factor of scaleRate*ChangeScaler per frame
            Vector3 newLocalScale = target.localScale*Mathf.Pow(1f + scaleRate*ChangeScaler, scaleStep);

            // Makes sure the new scale is within the determined range
            newLocalScale = C2M2.Utils.Math.Clamp(newLocalScale, minScale, maxScale);

            RelativeScale(target, pivot.transform.position, newLocalScale);
        }

        // Use to rescale Unity transform object from the perspective of a different pivot point
        private void RelativeScale(Transform target, Vector3 pivotPoint, Vector3 newScale)
        {
            // Calculate offset from pivot point and relative scale factor
            var localPivot = target.InverseTransformPoint(pivotPoint);

            // Calculate final position and rescale appropriately
            target.localScale = newScale;
            var newPivot = target.TransformPoint(localPivot);
            target.position += pivotPoint - newPivot;
        }

        // Determine if two lists of Vector3 objects are identical
        // Used to check if neurons have changed position since last rescale
        private static bool ContainsSameItems (List<Vector3> list1, List<Vector3> list2)
        {
            // If the lists don't have the same count, they can't be identical
            if (list1.Count != list2.Count) return false;
            // Otherwise, this uses SequenceEqual to compare the lists ordered by x, then y, then z coordinates
            return list1.OrderBy(v => v.x).ThenBy(v => v.y).ThenBy(v => v.z).SequenceEqual(list2.OrderBy(v => v.x).ThenBy(v => v.y).ThenBy(v => v.z));
        }

        private void SetNeuronParents(Transform transform)
        {
            if (neuronList.Length == 0) return;
            if (neuronList[0].transform.parent != transform)
            {
                for (int i = 0; i < neuronList.Count(); i++)
                {
                    neuronList[i].transform.SetParent(transform);
                }
            }
        }

        ///<returns>The Geometric Median of all of the active neurons</returns>
        private Vector3 GeometricMedian(Transform target)
        {
            // Update the list of neurons to include all objects tagged "SimulatedNeuronCell"
            // TODO: Create and track this in NDSimulationLoader by adding neurons to a list as they're generated
            int activeNeurons = neuronList.Count();

            // If there are no neurons, return the current median
            if (activeNeurons == 0)
            {
                return median;
            }

            // If there is only one neuron, geometric median is its position
            if (activeNeurons == 1)
            {
                median = neuronList[0].transform.position;
                return median;
            }

            // Initialize variables for neuron positions and temporary median value
            Vector3 tempMedian = Vector3.zero;
            List<Vector3> currentNeuronPositions = new List<Vector3>(new Vector3[activeNeurons]);
            List<Vector3> currentLocalNeuronPositions = new List<Vector3>(new Vector3[activeNeurons]);

            // Initialize tempMedian to the centroid (average position) while determining if neurons have moved since last rescale
            for (int i = 0; i < activeNeurons; i++)
            {
                // Add current neuron positions and global positions to the previously initialized lists
                currentNeuronPositions[i] = neuronList[i].transform.position;
                currentLocalNeuronPositions[i] = neuronList[i].transform.localPosition;

                if (i == activeNeurons-1) // If we've iterated through all neurons
                {
                    // If the neurons have the same global positions as the neurons from the previous call
                    if (ContainsSameItems(currentLocalNeuronPositions, neuronPositions))
                    {
                        return median; // Return and use previous geometric median to rescale
                    }
                    else neuronPositions = currentLocalNeuronPositions; // Otherwise store changed global neuron positions
                }
                tempMedian += currentNeuronPositions[i]; // tempMedian becomes the sum of coordinate values of all neurons
            }
            tempMedian /= (activeNeurons); // tempMedian is now the average of neuron coordinates
            /*
            // Below is an implementation of Weiszfeld's Algorithm for iterative numerical approximation of Geometric Median
            // The current code uses the Geometric Midpoint rather than the Median, but the below code can be uncommented and toggled on as desired

            const int maxIterations = 10;
            const float tolerance = 1e-5f;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                Vector3 numerator = Vector3.zero;
                float denominator = 0;

                for (int i = 0; i < activeNeurons; i++)
                {
                    Vector3 pos = neuronList[i].transform.position;
                    float dist = Vector3.Distance(tempMedian, pos); // Distance between each neuron and last approximation

                    if (dist != 0)
                    {
                        numerator += pos / dist;
                        denominator += 1 / dist;
                    }
                }

                // New approximation is determined
                Vector3 newMedian = numerator / denominator;

                // Check if it's within the tolerance threshold for adjustments
                if (Vector3.Distance(newMedian, tempMedian) < tolerance)
                {
                    median = newMedian;
                    return median;
                }
                // Otherwise store it as the new approximation, and continue to next iteration
                else tempMedian = newMedian;
                // After iteration limit, use most recent approximation even if tolerance hasn't been met
            }*/

            // Set median value to the calculated median
            median = tempMedian;
            return median;
        }
    }
}