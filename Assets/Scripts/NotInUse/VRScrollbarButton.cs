using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRScrollbarButton : MonoBehaviour
{
    public float scrollSpeed = 0.015f;
    private ScrollRect scrollRect;
    // Start is called before the first frame update
    void Start()
    {
        scrollRect = transform.parent.GetComponentInChildren<ScrollRect>(true);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void scrollUp()
    {
        scrollRect.verticalNormalizedPosition += scrollSpeed;
        Debug.Log(scrollRect.verticalNormalizedPosition);
    }

    public void scrollDown()
    {
        scrollRect.verticalNormalizedPosition -= scrollSpeed;
        Debug.Log(scrollRect.verticalNormalizedPosition);
    }
}
