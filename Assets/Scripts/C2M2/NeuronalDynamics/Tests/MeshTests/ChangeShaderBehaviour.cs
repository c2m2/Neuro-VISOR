using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.NeuronalDynamics.Tests
{
    /// <summary>
    /// Changes the shader for the selected gameobject, here: a 3d Mesh.
    /// If no shader is found, error will be reported to console only.
    /// </summary>
    public class ChangeShaderBehaviour : MonoBehaviour
    {
        /// Shader type we want to use for the curernt gameobject this script is attached to
        public string shaderType = "RevealBackfaces";
        
        /// Start
        void Start() {
            Renderer rend =  GetComponent<Renderer>();
            Shader shader =  Shader.Find ($"Custom/{shaderType}");
            /// Check for shader presence
            if (shader == null) {
                UnityEngine.Debug.LogError($"Selected shader not found: Custom/{shaderType}");
            }
            
            /// Check for rendered presence
            if (rend == null) {
                UnityEngine.Debug.LogError ("No renderer was found or associated with this gameobject.");
            }
            
            /// Change finally the shader
            rend.material.shader = shader;
        }
    }
}
