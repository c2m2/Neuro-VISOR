using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Attributes;

/// <summary>
/// Changes the shader for the selected gameobject, here: 3d Mesh.
/// If no shader is found, error will be reported to console only.
/// </summary>
namespace C2M2 {
    namespace Tests {
        public class ChangeShaderBehaviour : MonoBehaviour
        {
            public string shaderType = "RevealBackfaces";

            void Start() {
                Renderer rend =  GetComponent<Renderer> ();
                Shader shader =  Shader.Find ( $"Custom/{shaderType}" );
                if ( shader == null ) {
                    UnityEngine.Debug.LogError ( "Shader not found" );
                }
                if ( rend == null ) {
                    UnityEngine.Debug.LogError ( "Renderer not found" );
                }
                rend.material.shader = shader;
            }
        }

    }
}