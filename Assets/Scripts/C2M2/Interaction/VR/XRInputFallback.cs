using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Helper that reads controller state via Unity's XR InputDevices API, with a fallback to
/// the legacy Oculus OVRInput stack. This lets code originally written against OVRInput
/// keep working under OpenXR / Unity 6 where OVRPlugin.dll may fail to load (which causes
/// every OVRInput.* call to silently return zero).
///
/// Intentionally placed in the global namespace so the Oculus SDK's OVRGrabber.cs (also in
/// the global namespace) can call it without additional using directives.
/// </summary>
public static class XRInputFallback
{
    private static readonly List<InputDevice> deviceBuffer = new List<InputDevice>();

    private static XRNode NodeFor(OVRInput.Controller controller)
    {
        // Treat the "L*"/"R*" controller enums as the corresponding XR hand. For the
        // generic Touch/Active/All variants we fall back to right-hand as a best guess;
        // OVRGrabber only ever passes LTouch or RTouch in practice.
        switch (controller)
        {
            case OVRInput.Controller.LTouch:
            case OVRInput.Controller.LHand:
                return XRNode.LeftHand;
            default:
                return XRNode.RightHand;
        }
    }

    private static bool TryGetDevice(OVRInput.Controller controller, out InputDevice device)
    {
        InputDevices.GetDevicesAtXRNode(NodeFor(controller), deviceBuffer);
        if (deviceBuffer.Count > 0)
        {
            device = deviceBuffer[0];
            return true;
        }
        device = default;
        return false;
    }

    public static float GetGrip(OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device) &&
            device.TryGetFeatureValue(CommonUsages.grip, out float grip))
        {
            return grip;
        }
        return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
    }

    public static Vector3 GetLocalPosition(OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device) &&
            device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
        {
            return pos;
        }
        return OVRInput.GetLocalControllerPosition(controller);
    }

    public static Quaternion GetLocalRotation(OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            return rot;
        }
        return OVRInput.GetLocalControllerRotation(controller);
    }

    public static Vector3 GetLocalVelocity(OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device) &&
            device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 vel))
        {
            return vel;
        }
        return OVRInput.GetLocalControllerVelocity(controller);
    }

    public static Vector3 GetLocalAngularVelocity(OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device) &&
            device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 ang))
        {
            return ang;
        }
        return OVRInput.GetLocalControllerAngularVelocity(controller);
    }

    public static bool GetButton(OVRInput.Button button, OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device))
        {
            if (TryReadButton(device, button, out bool value)) return value;
        }
        return OVRInput.Get(button, controller);
    }

    // Tracks per-device button state between frames so we can synthesize "GetDown" from the
    // level-triggered InputDevices feature reads.
    private static readonly Dictionary<int, Dictionary<OVRInput.Button, bool>> prevButtonState
        = new Dictionary<int, Dictionary<OVRInput.Button, bool>>();

    public static bool GetButtonDown(OVRInput.Button button, OVRInput.Controller controller)
    {
        if (TryGetDevice(controller, out var device))
        {
            if (TryReadButton(device, button, out bool isPressed))
            {
                int key = (int)controller;
                if (!prevButtonState.TryGetValue(key, out var map))
                {
                    map = new Dictionary<OVRInput.Button, bool>();
                    prevButtonState[key] = map;
                }
                map.TryGetValue(button, out bool wasPressed);
                map[button] = isPressed;
                return isPressed && !wasPressed;
            }
        }
        return OVRInput.GetDown(button, controller);
    }

    private static bool TryReadButton(InputDevice device, OVRInput.Button button, out bool value)
    {
        switch (button)
        {
            case OVRInput.Button.One:             // A / X
                return device.TryGetFeatureValue(CommonUsages.primaryButton, out value);
            case OVRInput.Button.Two:             // B / Y
                return device.TryGetFeatureValue(CommonUsages.secondaryButton, out value);
            case OVRInput.Button.PrimaryIndexTrigger:
            case OVRInput.Button.SecondaryIndexTrigger:
                return device.TryGetFeatureValue(CommonUsages.triggerButton, out value);
            case OVRInput.Button.PrimaryHandTrigger:
            case OVRInput.Button.SecondaryHandTrigger:
                return device.TryGetFeatureValue(CommonUsages.gripButton, out value);
            case OVRInput.Button.PrimaryThumbstick:
            case OVRInput.Button.SecondaryThumbstick:
                return device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out value);
            case OVRInput.Button.Start:
                return device.TryGetFeatureValue(CommonUsages.menuButton, out value);
            default:
                value = false;
                return false;
        }
    }
}
