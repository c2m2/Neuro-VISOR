using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace C2M2.Interaction.UI
{
    /// <summary>
    /// Provides a method so that a button event can close Point Info Panels
    /// </summary>
    public class CloseAllPointInfoPanels : MonoBehaviour
    {
        public void CloseAllPanels()
        {
            PointInfo[] panels = GetComponentsInChildren<PointInfo>();
            if (panels.Length > 0) { foreach (PointInfo pi in panels) { pi.Close(); } }
        }
    }
}
