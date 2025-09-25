using UnityEngine;
using System.Collections.Generic;
using System;

namespace C2M2.SomaPositionCalculation
{
    /*************************************************************************************************************
    SPAWN ZONE CLASS

    Class defining the 3D space that somas are allowed to be automatically placed in (by SomaPositionCalculator).
    The center point is the default "target" for SomaPosition objects, which means they will be placed as close 
    to them as possible.

    @author: Adam Marx
    @date: 2025-04-28
    *************************************************************************************************************/
    public class SpawnZone
    {
        /** PROPERTIES *********************************************************************************************/
        // Dimensions
        public static float Width { get; private set; } = DEFAULT_WIDTH;         // X length
        public static float Height { get; private set; } = DEFAULT_HEIGHT;       // Y length
        public static float Depth { get; private set; } = DEFAULT_DEPTH;         // Z length
        public static Vector3 Location { get; private set; } = new Vector3(-1f, -0.5f, -0.5f); // Location of the spawn zone

        // Dimension bounds
        private float XMin => Location.x;
        private float XMax => Location.x + Width;
        private float YMin => Location.y;
        private float YMax => Location.y + Height;
        private float ZMin => Location.z;
        private float ZMax => Location.z + Depth;

        // Constants / read only
        private const float DEFAULT_WIDTH = 1.5F;
        private const float DEFAULT_HEIGHT = 1.5F;
        private const float DEFAULT_DEPTH = 2;

        // Other properties
        public Vector3 CenterPoint { get; private set; }    // Center point of the spawn zone
        public bool IsFull {get; set;} = false;             // Can more somas be placed? default to false 


        /** CONSTRUCTOR *****************************************************************************************/
        public SpawnZone()
        {
            CenterPoint = CalculateCenterPoint();
        }


        /** BOOL CHECKS *********************************************************************************************/
        // Is a point within the bounds of the spawn zone?
        public bool Contains(Vector3 position) 
        {
            float x = position.x;
            float y = position.y;
            float z = position.z;

            return x >= XMin && x <= XMax &&
                y >= YMin && y <= YMax &&
                z >= ZMin && z <= ZMax;
        }

        // Is a characteristic distance too long for the spawn zone?
        public bool CanContainLength(double charDist)
        {
            return charDist <= Width/2 || charDist <= Height/2 || charDist <= Depth/2;
        }


        /** ACCESSORS *******************************************************************************************/
        public float GetSmallestDimension() 
        {
            return Math.Min(Math.Min(Width, Height), Depth);
        }


        /** HELPERS *********************************************************************************************/
        private Vector3 CalculateCenterPoint() 
        {
            return new Vector3(
                Location.x + Width / 2f,
                Location.y + Height / 2f,
                Location.z + Depth / 2f
            );
        }

        
        /** TESTING *********************************************************************************************/
        // Functions here should not be used except to test the spawn zone
        private void MakeCenterPointVisible() 
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            // Set its position and scale
            dot.transform.position = CenterPoint;
            dot.transform.localScale = Vector3.one * 0.05f;

            // Set the color to red
            Renderer renderer = dot.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
        }

        private void MakeSpawnZoneVisible() 
        {
            GameObject SZV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SZV.transform.position = CenterPoint;
            SZV.transform.localScale = new Vector3(Width, Height, Depth);
            Renderer renderer = SZV.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
            }
        }
    }
}
