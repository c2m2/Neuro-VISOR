using C2M2.NeuronalDynamics.Simulation;
using UnityEngine;

public abstract class NDInteractables : MonoBehaviour
{
    public NDSimulation simulation = null;
    public int FocusVert { get; set; } = -1;

    /// <summary>
    /// returns the 3D vertex that we have clicked on
    /// </summary>
    public Vector3 FocusPos
    {
        get { return simulation.Verts1D[FocusVert]; }
    }

    public GameObject highlightObj;

    /// <summary>
    /// Attempt to latch onto a given simulation
    /// </summary>
    public void AttachToSimulation(NDSimulation sim, int index)
    {
        if (simulation == null)
        {
            simulation = sim;

            transform.parent = simulation.transform;

            FocusVert = index;

            Place(index);
        }
    }

    public abstract void Place(int index);

    public void Highlight(bool highlight)
    {
        if (highlightObj != null) highlightObj.SetActive(highlight);
    }

    override public string ToString()
    {
        return name + " " + FocusVert;
    }
}
