using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace C2M2 {
    namespace Attributes {
        /// <summary>
        /// Custom property attribute to override UnityEditor labels of variables
        /// </summary>
        public class CustomLabel : PropertyAttribute
        {
            public readonly string label;

            public CustomLabel ( string label ) {
                this.label = label;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        ///
        /// </summary>
        [CustomPropertyDrawer ( typeof ( CustomLabel ) )]
        internal class ThisPropertyDrawer : PropertyDrawer
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="position"></param>
            /// <param name="property"></param>
            /// <param name="label"></param>
            public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label ) {
                try {
                    var propertyAttribute = attribute as CustomLabel;
                    if ( !IsArray ( property ) ) {
                        label.text = propertyAttribute.label;
                    } else {
                        Debug.LogWarningFormat (
                            "{0}(\"{1}\") doesn't support arrays ",
                            typeof ( CustomLabel ).Name,
                            propertyAttribute.label
                        );
                    }
                    EditorGUI.PropertyField ( position, property, label );
                } catch ( System.Exception ex ) {
                    Debug.LogException ( ex );
                }
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="property"></param>
            /// <returns></returns>
            private bool IsArray ( SerializedProperty property ) {
                string path = property.propertyPath;
                int idot = path.IndexOf ( '.' );
                if ( idot == -1 ) return false;
                return property.serializedObject.FindProperty ( path.Substring ( 0, idot ) ).isArray;

            }
        }
#endif
    }

}