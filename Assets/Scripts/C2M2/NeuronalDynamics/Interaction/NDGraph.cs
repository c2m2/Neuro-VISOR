using C2M2.Interaction;
using C2M2.NeuronalDynamics.Interaction.UI;
using UnityEngine;

[RequireComponent(typeof(GrabRescaler))]
[RequireComponent(typeof(NDLineGraph))]
public class NDGraph : NDInteractables
{
    public NDGraphManager GraphManager { get { return simulation.graphManager; } }

    private GrabRescaler grabRescaler;
    public NDLineGraph ndlinegraph;

    // Start is called before the first frame update
    void Awake()
    {
        grabRescaler = GetComponent<GrabRescaler>();
        ndlinegraph = GetComponent<NDLineGraph>();
    }

    // Update is called once per frame
    void Update()
    {
        if (simulation == null || GraphManager == null)
        {
            ndlinegraph.DestroyPlot();
        }
    }

    private void OnDestroy()
    {
        GraphManager.interactables.Remove(this);
    }

    public override void Place(int index)
    {
        if (FocusVert == -1)
        {
            Debug.LogError("Invalid vertex given to NDLineGraph");
            Destroy(this);
        }
        name = "LineGraph(" + simulation.name + ")[vert" + FocusVert + "]";
        ndlinegraph.SetUp();

        GraphManager.interactables.Add(this);
    } //TO DO

    protected override void AddHitEventListeners()
    {
        HitEvent.OnHover.AddListener((hit) => grabRescaler.Rescale());
    }
}
