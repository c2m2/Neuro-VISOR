using C2M2.NeuronalDynamics.Interaction.UI;
using UnityEngine;

public class NDGraph : NDInteractables
{
    public NDGraphManager GraphManager { get { return simulation.graphManager; } }

    public NDLineGraph ndlinegraph;

    // Start is called before the first frame update
    void Start()
    {
        
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
    } //TO DO
}
