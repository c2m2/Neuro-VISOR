using C2M2.NeuronalDynamics.Simulation;
using UnityEngine;

public class RulerMeasure : MonoBehaviour
{
    public NDSimulation sim;
    private Vector3 trueSize;
    private Vector3 localSize;
    private float rulerLength;

    // Start is called before the first frame update
    void Start()
    {
        trueSize = sim.VisualMesh.bounds.size;
        localSize = sim.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        trueSize = sim.VisualMesh.bounds.size;
        Debug.Log("true " + trueSize);
        localSize = sim.transform.localScale;
        Debug.Log("local " + localSize);
        rulerLength = transform.lossyScale.z;
        Debug.Log("rulerLength " + rulerLength);
    }

    // returns rulers length relative the the mesh
    public float ReturnRulerMeshLength()
    {
        return Vector3.Dot(trueSize, localSize)/rulerLength;
    }
}
