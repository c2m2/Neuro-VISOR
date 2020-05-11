using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRScrollbar : MonoBehaviour
{
    PanelManager info;
    // Start is called before the first frame update
    void Start()
    {
        info = GetComponentInParent<PanelManager>();
    }

    // Update is called once per frame
    void Update()
    {
        //If there are fewer buttons on this panel than space in the vertical layout group, then we don't need a scroll bar
    }
}
