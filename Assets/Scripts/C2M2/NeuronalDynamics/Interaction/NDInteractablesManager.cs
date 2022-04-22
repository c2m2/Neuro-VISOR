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

    protected T preview = null;

    public float HoldCount { get; set; } = 0;

    public OVRInput.Button cancelCommand = OVRInput.Button.Two;
    public OVRInput.Button cancelCommandS = OVRInput.Button.Four;
    public KeyCode cancelKey = KeyCode.Backspace;
    public bool PressedCancel
    {
        get
        {
            if (GameManager.instance.vrDeviceManager.VRActive) return OVRInput.Get(cancelCommand) || OVRInput.Get(cancelCommandS);
            else return Input.GetKey(cancelKey);
        }
    }

    /// <summary>
    /// Hold down a raycast for this many seconds in order to destroy a clamp
    /// </summary>
    public int DestroyCount = 1;

    // Start is called before the first frame update
    void Awake()
    {
        HitEvent = gameObject.GetComponent<RaycastPressEvents>();
        AddHitEventListeners();
    }

    private void OnDestroy()
    {
        RemoveAll();
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
        DestroyPreview();

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

    public void Preview(RaycastHit hit)
    {

        // If we haven't already created a preview clamp, create one
        if (preview == null)
        {
            preview = InstantiateNDInteractable(hit);

            // If we couldn't build a preview, don't try to preview the position hit
            if (preview == null) return;

            foreach (Collider col in preview.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
            preview.SwitchMaterial(preview.previewMaterial);
            preview.name = "Preview";
            PreviewCustom();
        }

        preview.gameObject.SetActive(true);
    }

    protected abstract void PreviewCustom();

    public void DestroyPreview()
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
