using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2
{
    namespace Tests
    {
        public abstract class Test : MonoBehaviour
        {
            public abstract bool PreTest();
            public abstract bool RunTest();
            public abstract bool PostTest();
        }
    }
}
