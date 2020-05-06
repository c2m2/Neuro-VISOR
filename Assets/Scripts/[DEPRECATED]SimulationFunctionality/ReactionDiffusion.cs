#pragma warning disable CS0618

using System.Collections.Generic;

namespace C2M2
{
    using Utilities;
    using static Utilities.MathUtilities;
    /// <summary> Simulate a diffusion over the surface of a given geometry </summary>
    public class ReactionDiffusion : SimulationDiffuseReact
    {
        private DiffusionManager diffusionManager;

        #region ReactionDiffusionControls
        /// <summary> U^* = U^n + (ΔU^n)dt; ΔU^n = diffusionConstant(U^n - V^n), where V is a vert that shares an edge with U" </summary>
        public double diffusionConstant { get; set; }
        ///<summary> U^{n+1} = (reactionConstant)(U^*)(U^* - beta)(1 - U^*) </summary>
        public double reactionConstant { get; set; }
        ///<summary> U^{n+1} = (reactionConstant)(U^*)(U^* - beta)(1 - U^*) </summary>
        public double beta { get; set; }

        #endregion
        #region Utilities

        /// <summary>Initially a local copy of adjacencyListManager.componentData; this is the live list of simulation values for each unique vertex </summary>
        //public double[] simulationConditions { get; private set; }
        private MeshInfo meshInfo;
        /// <summary> List of all edges v1 -> v2 on the geometry; local pointer to adjacencyListManager.edgeList </summary>
        private List<Edge> edgeList;
        /// <summary> Gives the number of edges that each vertex belongs to. edgeCount[0] corresponds to adjacencyList[0] & scalars[0] </summary>
        private int[] edgeCount;
        /// <summary> Local pointer to adjacencyListManager.adjacencyList </summary>
        private List<Node>[] adjacencyList;
        /// <summary> Stores a flux along each edge in edgeList </summary>
        private double[] flux;
        #region PauseFunctionality

        private RaycastInputField inputField;
        #endregion
        #endregion

        protected override void Initialize()
        {
            diffusionManager = objectManager.diffusionManager;
            meshInfo = objectManager.meshInfo;
            adjacencyList = meshInfo.adjacencyList;
            edgeList = meshInfo.edgeList;
            simulationConditions = new double[meshInfo.scalars.Length];
            flux = new double[edgeList.Count];
            edgeCount = meshInfo.edgeCount;
        }
        double c;
        /// <summary> For each neighbor of each vertex, subtract some amount from the value of the vertex and add it to the neighbor. </summary>
        protected override double[] Solve(double dtReal)
        {
            //simulationConditions = meshInfo.scalars;
            c = dtReal * diffusionConstant;
            // Compute flux for each edge
            for (int i = 0; i < edgeList.Count; i++)
            {
                // c < (1 / # of v1 edges) & c < (1 / # of v2 edges)
                c = Min(c, Min((1d / edgeCount[edgeList[i].v1]), (1d / edgeCount[edgeList[i].v2])));
                flux[i] = c * (simulationConditions[edgeList[i].v1] - simulationConditions[edgeList[i].v2]);
            }
            // Diffusion update
            for (int i = 0; i < edgeList.Count; i++)
            {
                simulationConditions[edgeList[i].v1] -= flux[i];
                simulationConditions[edgeList[i].v2] += flux[i];
            }
            // Reaction Update
            for (int i = 0; i < simulationConditions.Length; i++)
            {
                simulationConditions[i] += (dtReal * R(simulationConditions[i]));
            }
            return simulationConditions;
        }
        public void ValuesRandomize()
        {
            simulationConditions.FillArrayRandom(0f, 1f);
            meshInfo.SubmitComponentDataChanges(simulationConditions.ToFloat());
        }
        public void ValuesEmpty()
        {
            simulationConditions.FillArray(0);
            meshInfo.SubmitComponentDataChanges(simulationConditions.ToFloat());
        }
        /// <summary> Change the current value at a specific point </summary>
        /// <param name="index"> UNIQUE index of the point to change (attaches to adjacencyList.uniqueVerts </param>
        /// <param name="value"> Value to add at this point </param>
        public void AddDiffusionCondition(int index, double value)
        {
            if (index < simulationConditions.Length)
            {
                simulationConditions[index] += value;
            }
            meshInfo.SubmitComponentDataChanges(simulationConditions.ToFloat());
        }
        /// <summary> Calculate Reaction term </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        private double R(double u) => (reactionConstant * u * (u - beta) * (1 - u));
    }
}