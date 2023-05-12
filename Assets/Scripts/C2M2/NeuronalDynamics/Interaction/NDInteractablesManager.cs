using C2M2;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using System.Collections.Generic;
using UnityEngine;

public abstract class NDInteractablesManager<T> : MonoBehaviour
    where T:NDInteractables
{
    public GameObject previewPrefab = null;

    public RaycastPressEvents HitEvent { get; protected set; } = null;

    private GameObject preview = null;

    public float HoldCount { get; set; } = 0;

    /// <summary>
    /// Hold down a raycast for this many seconds in order to destroy a clamp
    /// </summary>
    public float DestroyCount = 2f;
    
    // Hold down a raycast for this many seconds to change the synapse model
    public float ChangeCount = .75f;

    // Start is called before the first frame update
    void Awake()
    {
        HitEvent = gameObject.GetComponent<RaycastPressEvents>();
        AddHitEventListeners();
    }

    #region InputButtons

    /// <summary>
    /// Pressing these buttons allows interaction with the interactable
    /// </summary>
    public OVRInput.Button interactOVR = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button interactOVRS = OVRInput.Button.SecondaryIndexTrigger;
    public bool InteractHold
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
                return OVRInput.Get(interactOVR) || OVRInput.Get(interactOVRS);
            else return true;
        }
    }

    public KeyCode powerIncreaseKey = KeyCode.UpArrow;
    public KeyCode powerDecreaseKey = KeyCode.DownArrow;
    public float PowerModifier
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                // Uses the value of both joysticks added together
                float scaler = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

                return scaler;
            }
            else
            {
                if (Input.GetKey(powerIncreaseKey)) return .4f;
                if (Input.GetKey(powerDecreaseKey)) return -.4f;
                else return 0;
            }
        }
    }

    public OVRInput.Button highlightOVR = OVRInput.Button.PrimaryHandTrigger;
    public OVRInput.Button highlightOVRS = OVRInput.Button.SecondaryHandTrigger;
    public bool HighLightHold
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
                return OVRInput.Get(highlightOVR) || OVRInput.Get(highlightOVRS);
            else return false; // We cannot highlight through the emulator
        }
    }

    #endregion

    #region Behavior

    private void AddHitEventListeners()
    {
        HitEvent.OnHover.AddListener((hit) => Preview(hit));
        HitEvent.OnHoverEnd.AddListener((hit) => DestroyPreview());
        HitEvent.OnPress.AddListener((hit) => InstantiateNDInteractable(hit));
    }

    public T InstantiateNDInteractable(RaycastHit hit)
    {
        NDSimulation currentSimulation = hit.collider.GetComponentInParent<NDSimulation>();
        if (currentSimulation != null)
        {
            // Destroy any existing preview
            DestroyPreview();

            // Find the 1D vertex that we hit
            int index = currentSimulation.GetNearestPoint(hit);

            if (VertexAvailable(currentSimulation, index))
            {
                GameObject prefab = IdentifyBuildPrefab(currentSimulation, index);
                T interact;
                //TODO remove this
                if (typeof(T).Equals(typeof(NDGraph))) interact = Instantiate(prefab).GetComponent<T>();
                else interact = Instantiate(prefab, currentSimulation.transform).GetComponent<T>();
                interact.AttachToSimulation(currentSimulation, index);
                return interact;
            }
        }
        return null;
    }

    public abstract GameObject IdentifyBuildPrefab(NDSimulation sim, int index);

    public abstract bool VertexAvailable(NDSimulation sim, int index);

    public void Preview(RaycastHit hit)
    {
        // If we haven't already created a preview interactable, create one
        if (preview == null)
        {
            NDSimulation currentSimulation = hit.collider.GetComponentInParent<NDSimulation>();
            if (currentSimulation == null) return;

            // Find the 1D vertex that we hit
            int index = currentSimulation.GetNearestPoint(hit);

            if (VertexAvailable(currentSimulation, index))
            {
                preview = Instantiate(previewPrefab, currentSimulation.transform);

                float radius = (float)(3 * currentSimulation.VisualInflation * currentSimulation.Neuron.nodes[index].NodeRadius);
                
                preview.transform.localScale = new Vector3(radius, radius, radius);
                preview.transform.localPosition = currentSimulation.Verts1D[index];
            }
        }
    }

    public void DestroyPreview()
    {
        if (preview != null)
        {
            Destroy(preview.gameObject);
            preview = null;
        }
    }

    #endregion
}
