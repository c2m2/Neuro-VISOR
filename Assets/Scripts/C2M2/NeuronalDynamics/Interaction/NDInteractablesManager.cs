using C2M2;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using System.Collections.Generic;
using UnityEngine;

public abstract class NDInteractablesManager<T> : MonoBehaviour
    where T:NDInteractables
{
    public NDSimulation currentSimulation = null;
    public List<T> interactables = new List<T>();

    public RaycastPressEvents HitEvent { get; protected set; } = null;

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

    private void OnEnable()
    {
        /* Trigger events used for raycasting to the neuron
         * 
         * hitEvent is a refrence to the RaycastPressEvents script.
         * Which allows us to use predefined ray casting methods */
        HitEvent = gameObject.AddComponent<RaycastPressEvents>();
        AddHitEventListeners();
    }

    private void OnDisable()
    {
        // This prevents adding many RaycastPressEvents scripts each time user enables() this script
        Destroy(GetComponent<RaycastPressEvents>());
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

    protected abstract void AddHitEventListeners();

    public T InstantiateNDInteractable(RaycastHit hit)
    {
        currentSimulation = hit.collider.GetComponentInParent<NDSimulation>();
        if (currentSimulation == null) return null;

        // Destroy any existing preview
        DestroyPreview(hit);

        // Find the 1D vertex that we hit
        int index = currentSimulation.GetNearestPoint(hit);

        if (VertexAvailable(index))
        {
            GameObject prefab = IdentifyBuildPrefab(index);

            T interact = Instantiate(prefab, currentSimulation.transform).GetComponent<T>();

            interact.AttachToSimulation(currentSimulation, index);

            return interact;
            //TODO interactables.Add(graph);
        }
        //TO DO holdCount = 0;
        return null;
    }

    public abstract GameObject IdentifyBuildPrefab(int index);

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
            Destroy(preview.gameObject);
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
