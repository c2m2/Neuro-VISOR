using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


///<summary>
/// If an object enters this button's trigger collider, and
/// if that object is marked as the index finger, then
/// invoke the attached button's functionality
/// </summary>

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Collider))]
public class VRButton : MonoBehaviour
{
    [Tooltip("The time that this button should deactivate the fingterip collider for")]
    public float waitTime = 0.5f;
    [Tooltip("Run button function when finger enters button space")]
    public bool clickOnEnter = true;
    [Tooltip("Run button function each fram that finger remains in button space")]
    public bool clickOnStay = false;
    [Tooltip("Run button function when finger exit button space")]
    public bool clickOnExit = false;
    [Tooltip("Deactivate fingertip collider after pressing enter button")]
    public bool deactivateOnEnter = true;
    [Tooltip("Deactivate fingertip collider after pressing stay button")]
    public bool deactivateOnStay = false;
    [Tooltip("Deactivate fingertip collider after pressing exit button")]
    public bool deactivateOnExit = false;

    private Button button;
    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (clickOnEnter)
        {
            if(other.tag == "IndexFinger")
            {
                if (deactivateOnEnter)
                {
                   // other.GetComponent<IndexTipManager>().RequestTimedDeactivate(0.5f);     //Disable fingertip collider
                }

                button.onClick.Invoke();
            }
        }

    }

    private void OnTriggerStay(Collider other)
    {
        if (clickOnStay)
        {
            if (other.tag == "IndexFinger")
            {
                if (deactivateOnStay)
                {
                   // other.GetComponent<IndexTipManager>().RequestTimedDeactivate(0.5f);     //Disable fingertip collider
                }

                button.onClick.Invoke();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (clickOnExit)
        {
            if (other.tag == "IndexFinger")
            {
                if (deactivateOnExit)
                {
                   // other.GetComponent<IndexTipManager>().RequestTimedDeactivate(0.5f);     //Disable fingertip collider
                }

                button.onClick.Invoke();
            }
        }
    }


}
