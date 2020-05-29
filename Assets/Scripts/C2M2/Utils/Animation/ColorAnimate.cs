using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Utils.Animation
{
    /// <summary>
    /// Alters the color of a material in a sinusoidal fashion using FixedUpdate
    /// </summary>
    public class ColorAnimate : MonoBehaviour
    {
        public Color altColor = Color.black;
        public Renderer rend;

        void Start()
        {
            //Get the renderer of the object so we can access the color
            rend = GetComponent<Renderer>();
            //Set the alt
            altColor = rend.material.color;
        }

        void FixedUpdate()
        {
            altColor.r = (Mathf.Sin(Mathf.PI * Time.time / 10) + 1) / 2;

            rend.material.color = altColor;

        }
    }
}
