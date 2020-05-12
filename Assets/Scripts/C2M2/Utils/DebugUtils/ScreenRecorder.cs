using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GetSocialSdk.Capture.Scripts;
using System;

public class ScreenRecorder : MonoBehaviour
{
    public KeyCode recordKey = KeyCode.Space;
    private GetSocialCapture screenRecorder;
    // Start is called before the first frame update
    void Awake()
    {
        screenRecorder = Camera.main.gameObject.GetComponent<GetSocialCapture>();
        if(screenRecorder == null)
        {
            Debug.LogWarning("No screen recorder found on " + Camera.main.name);
            Destroy(this);
        }
        screenRecorder.captureMode = GetSocialCapture.GetSocialCaptureMode.Manual;

    }

    // Update is called once per frame
    private bool recording = false;
    void Update()
    {
        if (IsPressed)
        {
            if (!recording)
            {
                screenRecorder.StartCapture();
                recording = true;
            }
            screenRecorder.CaptureFrame();
        }
    }
    private bool IsPressed
    {
        get
        {
            if (Input.GetKey(recordKey)) return true;
            return false;
        }
    }
    private void OnApplicationQuit()
    {
        if (recording)
        {         
            screenRecorder.StopCapture();
            // generate gif
            Action<byte[]> result = bytes =>
            {
                // Action<string> messageTarget = s => 
                // {
                //      Console.WriteLine(s)
                // }
                // generated gif returned as byte[]

                //ImageConversion.EncodeToPNG
                /* using (MemoryStream ms = new MemoryStream(gifContent))
                {
                    return Image.FromStream(ms);
                }*/
                byte[] gifContent = bytes;
                //Image image = Image.FromStream(ms4, true, true);
                //image.Save(@"C:\Users\Administrator\Desktop\imageTest.png", System.Drawing.Imaging.ImageFormat.Png);

                // use content, like send it to your friends by using GetSocial Sdk
            };

            screenRecorder.GenerateCapture(result);
        }
    }
}
