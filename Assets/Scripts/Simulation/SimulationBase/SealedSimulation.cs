using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2
{
    using Utilities;
    namespace SimulationScripts
    {
        /// <summary> Seals Awake/Start/Update so that derived classes can't hide parent classes' implementations. </summary>
        public class SealedSimulation : SealedMonoBehaviour
        {
            // Don't allow derived classes to override this.Awake/Start/Update
            public sealed override void Awake() { AwakeA(); }
            public sealed override void Start() { StartA(); }
            public sealed override void Update() { UpdateA(); }
            // Allow derived classes to run code in Awake/Start/Update if they choose
            protected virtual void AwakeA() { }
            protected virtual void StartA() { }
            protected virtual void UpdateA() { }
        }
    }
}
