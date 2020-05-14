using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRObjectResetButton : MonoBehaviour
{

    public AudioSource source;
    public Image BackgroundImage;
    public Color NormalColor;
    public Color HighlightColor;
    public GameObject ButtonObject;
    public AudioClip clickSound;
    private Vector3 startPos;
    private Quaternion startRot;

    // Use this for initialization
    void Start()
    {

        //Initialize position and rotation
        startPos = ButtonObject.transform.position;
        startRot = ButtonObject.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnGazeEnter()
    {
        BackgroundImage.color = HighlightColor;
    }

    public void OnGazeExit()
    {
        BackgroundImage.color = NormalColor;
    }

    public void OnClick()
    {

        //Revert postion and rotation
        ButtonObject.transform.position = startPos;
        ButtonObject.transform.rotation = startRot;

        //Play sound on click
        source.PlayOneShot(clickSound);

    }

}