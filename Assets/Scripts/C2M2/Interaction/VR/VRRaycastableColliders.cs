using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.VR {
    public class VRRaycastableColliders :VRRaycastable<Collider[]>
    {
        // protected Collider[] source;

        public override Collider[] GetSource()
        {
            return source;
        }

        public override void SetSource(Collider[] source)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnAwake()
        {
            throw new System.NotImplementedException();
        }
    }
}
