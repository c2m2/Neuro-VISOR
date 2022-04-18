using C2M2;
using C2M2.NeuronalDynamics.Simulation;
using System.Collections.Generic;
using UnityEngine;

public abstract class NDInteractablesManager<T> : MonoBehaviour
    where T:NDInteractables
{
    public NDSimulation currentSimulation = null;
    public List<T> interactables = new List<T>();

    public T preview = null;

    public bool powerClick { get; set; } = false;
    public float holdCount { get; set; } = 0;

    /// <summary>
    /// Hold down a raycast for this many seconds in order to destroy a clamp
    /// </summary>
    public int DestroyCount = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        
    }

    #region InputButtons

    /// <summary>
    /// Pressing these buttons allows interaction with the interactable
    /// </summary>
    public OVRInput.Button interactOVR = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button interactOVRS = OVRInput.Button.SecondaryIndexTrigger;
    public bool PressedInteract
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
                return (OVRInput.Get(interactOVR) || OVRInput.Get(interactOVRS));
            else return true;
        }
    }

    public OVRInput.Button highlightOVR = OVRInput.Button.PrimaryHandTrigger;
    public OVRInput.Button highlightOVRS = OVRInput.Button.SecondaryHandTrigger;
    public bool PressedHighlight
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
                return (OVRInput.Get(highlightOVR) || OVRInput.Get(highlightOVRS));
            else return false; // We cannot highlight through the emulator
        }
    }

    #endregion

    #region Behavior
    /*public void InitiateNDInteractable(RaycastHit hit)
    {
        currentSimulation = hit.collider.GetComponentInParent<NDSimulation>();
        if (currentSimulation != null) BuildNDInteractable(hit);
    }

    public NDInteractables BuildNDInteractable(RaycastHit hit)
    {
        // Make sure we have valid prefabs
        if (clampPrefab == null) Debug.LogError("No Clamp prefab found");
        if (somaClampPrefab == null) Debug.LogError("No Soma Clamp prefab found");

        // Destroy any existing preview
        DestroyPreview(hit);

        // Find the 1D vertex that we hit
        int index = currentSimulation.GetNearestPoint(hit);

        if (VertexAvailable(index))
        {
            // If this vertex is available, instantiate an interactable and attach it to the simulation
            NDInteractables interact;

            if (currentSimulation.Neuron.somaIDs.Contains(clampIndex)) clamp = Instantiate(somaClampPrefab, Simulation.transform).GetComponentInChildren<NeuronClamp>();
            interact = Instantiate(clampPrefab, currentSimulation.transform).GetComponentInChildren<NDInteractables>();

            interact.AttachToSimulation(currentSimulation, index);

            return interact;
        }
        return null;
    }*/

    public abstract bool VertexAvailable(int index);

    public void Remove(T NDInteractable)
    {
        interactables.Remove(NDInteractable);
        Destroy(NDInteractable);
    }

    public void RemoveAll()
    {
        if (interactables.Count > 0)
        {
            foreach (T interact in interactables)
            {
                if (interact != null) Remove(interact);
            }
        }
    }

    public void DestroyPreview(RaycastHit hit)
    {
        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }
    }

    public void HighlightAll(bool highlight)
    {
        if (interactables.Count > 0)
        {
            foreach (T interact in interactables)
            {
                interact.Highlight(highlight);
            }
        }
    }

    #endregion
}
