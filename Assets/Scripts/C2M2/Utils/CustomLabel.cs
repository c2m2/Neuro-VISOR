using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace C2M2.Utils.Editor
{
    /// <summary>
    /// Custom property attribute to override UnityEditor labels of variables
    /// </summary>
    public class CustomLabel : PropertyAttribute
    {
        /// label name
        public readonly string label;

        /// <summary>
        /// Constructs a custom label
        /// </summary>
        /// <param name="label"> Label text </param>
        public CustomLabel(string label) => this.label = label;
    }
}


namespace C2M2.Utils.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer for CustomLabel classes
    /// This will allow to add custom label text for properties in 
    /// the UnityEditor'S inspector. Also arrays of properties are supported.
    /// </summary>
    [CustomPropertyDrawer(typeof(CustomLabel))]
    internal class ThisPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Set the label text of a specified property during creation of GUI 
        /// </summary>
        /// <param name="position"> position on GUI </param>
        /// <param name="property"> GUI property </param>
        /// <param name="label"> Name of label to display</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                var propertyAttribute = attribute as CustomLabel;
                if (!IsArray(property))
                {
                    label.text = propertyAttribute.label;
                }
                else
                {
                    Debug.LogWarningFormat(
                        "{0}(\"{1}\") doesn't support arrays ",
                        typeof(CustomLabel).Name,
                        propertyAttribute.label
                    );
                }
                EditorGUI.PropertyField(position, property, label);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Helper method to check if or not a serializable property is of array type or not
        /// </summary>
        /// <param name="property">A property </param>
        /// <returns> bool </returns>
        private bool IsArray(SerializedProperty property)
        {
            string path = property.propertyPath;
            int idot = path.IndexOf('.');
            if (idot == -1) return false;
            return property.serializedObject.FindProperty(path.Substring(0, idot)).isArray;
        }
    }
#endif
}
