using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In addition to standard HMD emulation, allow camera movement
/// </summary>
public class OVRHeadsetEmulatorController : OVRHeadsetEmulator
{
    public KeyCode[] slowMoveKeys = new KeyCode[] { KeyCode.LeftShift, KeyCode.RightShift };

    public float movementSensitivity = 0.5f;
    public float rotationSensitivity = 1.5f;
    private float xAxisValue;
    private float zAxisValue;

    public IEnumerator ResolveMovement()
    {
        // Get key input state
        xAxisValue = Input.GetAxis("Horizontal");
        zAxisValue = Input.GetAxis("Vertical");
        // Apply input state to position
        if (IsSlowMoving()) transform.Translate(new Vector3(xAxisValue * 0.05f, 0.0f, zAxisValue * 0.05f));
        else transform.Translate(new Vector3(xAxisValue * 0.5f, 0.0f, zAxisValue * 0.5f));
        yield return null;
    }
    private bool IsSlowMoving()
    {
        foreach(KeyCode key in slowMoveKeys)
        {
            if (Input.GetKey(key)) return true;
        }
        return false;
    }
}
