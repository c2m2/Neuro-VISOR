using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.MolecularDynamics.Visualization
{
    public class SphereInstantiator : MonoBehaviour
    {
        public Transform[] InstantiateSpheres(Sphere[] spheres, string rootName = "Spheres", string instanceName = "Sphere")
        {
            Transform root = new GameObject().transform;
            root.parent = transform;
            root.name = rootName;
            root.localPosition = Vector3.zero;
            root.localEulerAngles = Vector3.zero;

            GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = sphereObj.GetComponent<MeshFilter>().mesh;

            // Instantiate sphere objects, adjust their transforms
            string fmt = instanceName + " {0}";
            Transform[] transforms = new Transform[spheres.Length];
            for (int i = 0; i < spheres.Length; i++)
            {
                // Todo, instantiate at position
                //transforms[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                GameObject obj = new GameObject();
                obj.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
                obj.AddComponent<MeshRenderer>().sharedMaterial = GameManager.instance.defaultMaterial;

                transforms[i] = obj.transform;
                transforms[i].position = spheres[i].position;
                float diameter = spheres[i].radius * 2;
                transforms[i].localScale = new Vector3(diameter, diameter, diameter);
                transforms[i].name = string.Format(fmt, i);
                transforms[i].parent = root;
            }

            // Cleanup
            Destroy(sphereObj);
            return transforms;
        }
        public Transform InstantiateChildedSpheres(Sphere[] spheres)
        {
            // Instantiate the given spheres
            Transform[] transforms = InstantiateSpheres(spheres);

            // Instantiate the child object
            Transform childTransform = (new GameObject("SphereParent")).transform;
            childTransform.transform.parent = transform;
            childTransform.transform.position = Vector3.zero;
            childTransform.transform.eulerAngles = Vector3.zero;

            // Make each sphere's parent the child
            for(int i = 0; i < transforms.Length; i++)
            {
                transforms[i].parent = childTransform;
            }

            // Return the child object
            return childTransform;
        }
    }
}
