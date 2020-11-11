using C2M2.Simulation;
using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Simulation<,,,>))]
public class RulerMeasure : MonoBehaviour
{
    public MeshSimulation sim = null;
    public TextMeshProUGUI numberValues;
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
        numberValues.text = ToString();
    }

    // returns rulers length relative the the mesh
    public float ReturnRulerMeshLength()
    {
        return rulerLength/localSize.z;
    }

    public override string ToString()
    {
        float relativeLength = ReturnRulerMeshLength();
        // returns ruler's 1/4, 1/2, 3/4, and full lengths in terms of the simulation
        return String.Format("{0:f2}     {1:f2}     {2:f2}    {3:f2}", relativeLength/4, relativeLength/2, 3*relativeLength/ 4, relativeLength);
    }
}
