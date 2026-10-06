using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public abstract class NDInteractables : MonoBehaviour
{
    public NDSimulation simulation = null;
    public int FocusVert { get; set; } = -1;
    public MeshRenderer meshRenderer;

    /// <summary>
    /// The real local radius of the rendered dendrite surface at FocusVert, sampled from the
    /// actual RaycastHit at placement time (NDSimulation.GetNearestPoint's out param) - 0 if
    /// this interactable was attached without a hit (e.g. restored from a save file via
    /// Menu.cs, with AttachToSimulation's radius parameter left at its default). See
    /// NeuronClamp.SetScale for why this exists instead of just using NodeData.NodeRadius.
    /// </summary>
    public float LocalMeshRadius { get; private set; } = 0f;

    public Material defaultMaterial = null;
    public Material previewMaterial = null;
    public Material destroyMaterial = null;

    public RaycastPressEvents HitEvent { get; protected set; } = null;

    /// <summary>
    /// returns the 3D vertex that we have clicked on
    /// </summary>
    public Vector3 FocusPos
    {
        get { return simulation.Verts1D[FocusVert]; }
    }

    public GameObject highlightObj;

    private void Awake()
    {
        HitEvent = gameObject.GetComponent<RaycastPressEvents>();
        AddHitEventListeners();
    }

    /// <summary>
    /// Attempt to latch onto a given simulation
    /// </summary>
    public void AttachToSimulation(NDSimulation sim, int index, float localMeshRadius = 0f)
    {
        if (simulation == null)
        {
            simulation = sim;
            FocusVert = index;
            LocalMeshRadius = localMeshRadius;
            Place(index);
        }
    }

    public abstract void Place(int index);

    protected abstract void AddHitEventListeners();

    public void Highlight(bool highlight)
    {
        if (highlightObj != null) highlightObj.SetActive(highlight);
    }

    public void SwitchMaterial(Material material)
    {
        if (material != null) meshRenderer.material = material;
    }

    override public string ToString()
    {
        return name + " " + FocusVert;
    }
}
