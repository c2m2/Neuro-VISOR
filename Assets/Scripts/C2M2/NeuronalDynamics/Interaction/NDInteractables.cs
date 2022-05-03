using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public abstract class NDInteractables : MonoBehaviour
{
    public NDSimulation simulation = null;
    public int FocusVert { get; set; } = -1;
    public MeshRenderer meshRenderer;

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
    public void AttachToSimulation(NDSimulation sim, int index)
    {
        if (simulation == null)
        {
            simulation = sim;
            FocusVert = index;
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
