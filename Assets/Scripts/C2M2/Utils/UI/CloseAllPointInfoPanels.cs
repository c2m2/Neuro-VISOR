using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Interaction.UI
{
    public class CloseAllPointInfoPanels : MonoBehaviour
    {
        public void CloseAllPanels()
        {
            PointInfo[] panels = GetComponentsInChildren<PointInfo>();
            if (panels.Length > 0) { foreach (PointInfo pi in panels) { pi.Close(); } }
        }
    }
}
