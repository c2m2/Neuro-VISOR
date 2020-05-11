#pragma warning disable CS0618

using UnityEngine;

namespace C2M2
{
    [RequireComponent(typeof(VTUManager))]
    [RequireComponent(typeof(MeshInfo))]
    [RequireComponent(typeof(DiffusionManager))]
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
            vtuManager.Initialize(this);
            meshInfo = GetComponent<MeshInfo>();
            meshInfo.gradient = gradient;
            meshInfo.Initialize(this);
            diffusionManager = GetComponent<DiffusionManager>();
            diffusionManager.Initialize(this);
        }
    }
}
