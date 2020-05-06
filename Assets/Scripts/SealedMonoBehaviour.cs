using UnityEngine;

namespace C2M2
{
    namespace Utilities
    {
        /// <summary>
        /// Provides a method for sealing base-class Awake, Start, & Update methods
        /// </summary>
        /// <remarks>
        /// Make your base class inherit from this class, and then mark its Awake/Start/Update as "sealed override"
        /// </remarks>
        public class SealedMonoBehaviour : MonoBehaviour
        {
            public virtual void Awake() { }
            public virtual void Start() { }
            public virtual void Update() { }
        }
    }
}