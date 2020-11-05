using C2M2.NeuronalDynamics.Simulation;
using UnityEngine;

[RequireComponent(typeof(NDSimulation))]
public class RulerMeasure : MonoBehaviour
{
    public NDSimulation sim = null;
    private Vector3 localSize;
    private float rulerLength;

    // Start is called before the first frame update
    void Start()
    {
        localSize = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        localSize = sim.transform.localScale;
        rulerLength = transform.lossyScale.z;
    }

    // returns rulers length relative the the mesh
    public float ReturnRulerMeshLength()
    {
        return rulerLength/localSize.z;
    }
}
