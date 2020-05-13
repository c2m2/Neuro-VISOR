using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.OIT
{
    public class DataPoint
    {

        public Vector3 position;
        public float scalarValue;
        public Color color;

        public DataPoint()
        {

        }

        public DataPoint(Vector3 position, float scalarValue, Color color)
        {
            this.position = position;
            this.scalarValue = scalarValue;
            this.color = color;
        }
    }
}
