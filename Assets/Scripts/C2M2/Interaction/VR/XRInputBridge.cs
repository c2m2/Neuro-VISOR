using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Singleton bridge that reads XR controller state via Unity's InputDevice /
    /// CommonUsages API.  This is device-agnostic: it works with Meta Quest Link,
    /// standalone Quest builds, and any other OpenXR runtime without needing an
    /// explicit interaction-profile binding configuration.
    /// </summary>
    public class XRInputBridge : MonoBehaviour
    {
        private static XRInputBridge _instance;
        public static XRInputBridge Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("XRInputBridge");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<XRInputBridge>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ── Cached device lists (reused each frame to avoid per-frame allocation) ──
        private readonly List<InputDevice> _leftDevices  = new List<InputDevice>();
        private readonly List<InputDevice> _rightDevices = new List<InputDevice>();

        // ── Current-frame state ────────────────────────────────────────────────────
        private bool    _leftTrigger,  _rightTrigger;
        private bool    _leftGrip,     _rightGrip;
        private bool    _leftPrimary,  _rightPrimary;
        private bool    _leftSecondary,_rightSecondary;
        private bool    _menu;
        private Vector2 _leftThumb,    _rightThumb;

        // ── Previous-frame state (for WasPressedThisFrame equivalents) ────────────
        private bool _prevLeftTrigger,  _prevRightTrigger;
        private bool _prevLeftPrimary,  _prevRightPrimary;
        private bool _prevMenu;

        private void Update()
        {
            // Snapshot previous
            _prevLeftTrigger  = _leftTrigger;
            _prevRightTrigger = _rightTrigger;
            _prevLeftPrimary  = _leftPrimary;
            _prevRightPrimary = _rightPrimary;
            _prevMenu         = _menu;

            // Refresh device handles
            _leftDevices.Clear();
            _rightDevices.Clear();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand,  _leftDevices);
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, _rightDevices);

            InputDevice l = _leftDevices.Count  > 0 ? _leftDevices[0]  : default;
            InputDevice r = _rightDevices.Count > 0 ? _rightDevices[0] : default;

            // Triggers: try boolean first, then float with 0.5 threshold
            _leftTrigger  = Bool(l, CommonUsages.triggerButton) || Float(l, CommonUsages.trigger) > 0.5f;
            _rightTrigger = Bool(r, CommonUsages.triggerButton) || Float(r, CommonUsages.trigger) > 0.5f;

            // Grips
            _leftGrip  = Bool(l, CommonUsages.gripButton) || Float(l, CommonUsages.grip) > 0.5f;
            _rightGrip = Bool(r, CommonUsages.gripButton) || Float(r, CommonUsages.grip) > 0.5f;

            // Face buttons
            _leftPrimary    = Bool(l, CommonUsages.primaryButton);    // X
            _rightPrimary   = Bool(r, CommonUsages.primaryButton);    // A
            _leftSecondary  = Bool(l, CommonUsages.secondaryButton);  // Y
            _rightSecondary = Bool(r, CommonUsages.secondaryButton);  // B

            // Menu (left controller)
            _menu = Bool(l, CommonUsages.menuButton);

            // Thumbsticks
            _leftThumb  = Vec2(l, CommonUsages.primary2DAxis);
            _rightThumb = Vec2(r, CommonUsages.primary2DAxis);
        }

        // ── Device-feature helpers ────────────────────────────────────────────────

        private static bool Bool(InputDevice d, InputFeatureUsage<bool> u)
        {
            if (!d.isValid) return false;
            d.TryGetFeatureValue(u, out bool v);
            return v;
        }

        private static float Float(InputDevice d, InputFeatureUsage<float> u)
        {
            if (!d.isValid) return 0f;
            d.TryGetFeatureValue(u, out float v);
            return v;
        }

        private static Vector2 Vec2(InputDevice d, InputFeatureUsage<Vector2> u)
        {
            if (!d.isValid) return Vector2.zero;
            d.TryGetFeatureValue(u, out Vector2 v);
            return v;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Index trigger held.</summary>
        public bool GetTrigger(bool isLeft)       => isLeft ? _leftTrigger  : _rightTrigger;

        /// <summary>Index trigger first pressed this frame.</summary>
        public bool GetTriggerDown(bool isLeft)   => (isLeft ? _leftTrigger  : _rightTrigger)
                                                   && !(isLeft ? _prevLeftTrigger : _prevRightTrigger);

        /// <summary>Either index trigger held.</summary>
        public bool GetEitherTrigger()            => _leftTrigger || _rightTrigger;

        /// <summary>Grip (middle-finger) trigger held.</summary>
        public bool GetGrip(bool isLeft)          => isLeft ? _leftGrip  : _rightGrip;

        /// <summary>Either grip held.</summary>
        public bool GetEitherGrip()               => _leftGrip || _rightGrip;

        /// <summary>Thumbstick axis value.</summary>
        public Vector2 GetThumbstick(bool isLeft) => isLeft ? _leftThumb  : _rightThumb;

        /// <summary>Sum of both thumbsticks' Y axes.</summary>
        public float GetBothThumbsticksY()        => _leftThumb.y + _rightThumb.y;

        /// <summary>Primary face button (A = right, X = left) held.</summary>
        public bool GetPrimaryButton(bool isLeft) => isLeft ? _leftPrimary  : _rightPrimary;

        /// <summary>Primary face button first pressed this frame.</summary>
        public bool GetPrimaryButtonDown(bool isLeft) => (isLeft ? _leftPrimary  : _rightPrimary)
                                                       && !(isLeft ? _prevLeftPrimary : _prevRightPrimary);

        /// <summary>Secondary face button (B = right, Y = left) held.</summary>
        public bool GetSecondaryButton(bool isLeft) => isLeft ? _leftSecondary : _rightSecondary;

        /// <summary>Menu button held (left controller).</summary>
        public bool GetMenuButton()    => _menu;

        /// <summary>Menu button first pressed this frame.</summary>
        public bool GetMenuButtonDown() => _menu && !_prevMenu;
    }
}
