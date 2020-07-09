using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction.VR {
    public class VRRaycastableColliders :VRRaycastable<Collider[]>
    {
        // protected Collider[] source;

        public override Collider[] GetSource() => source;

        public override void SetSource(Collider[] source)
        {
            if (source == null) Debug.LogError("Collider source is null");
            if (source.Length == 0) Debug.LogError("Collider source is empty");

            Collider[] copy = new Collider[source.Length];
            for(int i = 0; i < source.Length; i++)
            {
                GameObject target = BuildChildObject(source[i].transform);
                //BuildRigidBody(target);
                copy[i] = (Collider)CopyComponent(source[i], target);
                target.transform.localPosition = Vector3.zero;
                target.transform.eulerAngles = Vector3.zero;
                target.transform.localScale = Vector3.one;
            }
            this.source = copy;
        }

        protected override void OnAwake()
        {

        }

        private Component CopyComponent(Component original, GameObject destination)
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy;
        }
    }
}
