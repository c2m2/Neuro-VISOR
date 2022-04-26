using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    [System.Serializable]
    public class SynapseData
    {
        [System.Serializable]
        public struct SynData
        {
            public int synVert;
            public int simID;
        }

        public SynData[] syns;
    }
}
