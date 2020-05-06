using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    namespace Utilities
    {
        public class ComponentUtilities
        {
            /// <summary>
            /// Separate children from their parents
            /// </summary>
            /// <returns></returns>
            public T[] GetComponentsInChildrenOnly<T>(Transform parent) where T: Component
            { 
                T[] instances = parent.transform.parent.GetComponentsInChildren<T>();
                T[] actualChildren = new T[parent.transform.parent.childCount];
                int index = 0;
                // For each child,
                foreach (T instance in instances)
                {
                    // If it is actually a child, save it
                    if (instance.transform.parent == parent)
                    {
                        actualChildren[index] = instance;
                        index++;
                    }
                }
                return actualChildren;             
            }
        }
    }
}
