using UnityEditor;

namespace C2M2.NeuronalDynamics.Simulation
{
    [CustomEditor(typeof(NDSimulation), true)]
    public class NDSimulationEditor : Editor
    {
        static int refinementLevel;
        static double inflationLevel;

        NDSimulation sim;

        public void Awake()
        {
            sim = target as NDSimulation;
        }

        public override void OnInspectorGUI()
        {
            refinementLevel = EditorGUILayout.IntField("Refinement Level: ", sim.RefinementLevel);
            inflationLevel = EditorGUILayout.DoubleField("Inflation Level: ", sim.VisualInflation);

            sim.RefinementLevel = refinementLevel;
            sim.VisualInflation = inflationLevel;
            DrawDefaultInspector();
        }
    }
}