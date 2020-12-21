#pragma warning disable CS0618

using UnityEngine;
using System;
using C2M2.NeuronalDynamics.Visualization.VTK;

namespace C2M2.Simulation
{
    using Interaction;
   
    [RequireComponent(typeof(VTUManager))]
    [RequireComponent(typeof(MeshInfo))]
    [RequireComponent(typeof(DiffusionManager))]
    [Obsolete("Replaced by VTUManager")]
    public class ObjectManager : MonoBehaviour
    {
        public Gradient gradient;
        public VTUManager vtuManager { get; set; }
        public MeshInfo meshInfo { get; set; }
        public DiffusionManager diffusionManager { get; private set; }

        private void Awake()
        {
            vtuManager = GetComponent<VTUManager>();
            vtuManager.gradient = gradient;
            vtuManager.Initialize();
            meshInfo = GetComponent<MeshInfo>();
            meshInfo.gradient = gradient;
            meshInfo.Initialize(this);
            diffusionManager = GetComponent<DiffusionManager>();
            diffusionManager.Initialize(this);
        }
    }
}
