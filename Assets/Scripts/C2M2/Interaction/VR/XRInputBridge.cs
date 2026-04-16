using UnityEngine;
using UnityEngine.InputSystem;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Singleton bridge from the legacy OVRInput API to the Unity Input System.
    /// Each action carries multiple bindings covering both the generic XRController
    /// paths and the Oculus-specific paths so at least one will match the active
    /// OpenXR interaction profile (Meta Quest Link, built PC app, etc.).
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

        // ── Index triggers ──────────────────────────────────────────────────────
        private InputAction rightTrigger;
        private InputAction leftTrigger;

        // ── Grip (middle finger) ───────────────────────────────────────────────
        private InputAction rightGrip;
        private InputAction leftGrip;

        // ── Thumbsticks (analog Vector2) ───────────────────────────────────────
        private InputAction rightThumbstick;
        private InputAction leftThumbstick;

        // ── Face buttons ───────────────────────────────────────────────────────
        private InputAction rightPrimary;    // A button
        private InputAction leftPrimary;     // X button
        private InputAction rightSecondary;  // B button
        private InputAction leftSecondary;   // Y button

        // ── System buttons ────────────────────────────────────────────────────
        private InputAction menuButton;      // Oculus / Meta button (left controller)

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Each critical action gets both the generic XRController float-axis path
            // and the Oculus-specific boolean-button path as fallbacks.  Unity Input
            // System will bind whichever path matches the active device layout.

            rightTrigger   = MakeMultiButton("RightTrigger",
                "<XRController>{RightHand}/trigger",
                "<XRController>{RightHand}/triggerButton",
                "<OculusTouchController>{RightHand}/trigger",
                "<OculusTouchController>{RightHand}/triggerPressed");

            leftTrigger    = MakeMultiButton("LeftTrigger",
                "<XRController>{LeftHand}/trigger",
                "<XRController>{LeftHand}/triggerButton",
                "<OculusTouchController>{LeftHand}/trigger",
                "<OculusTouchController>{LeftHand}/triggerPressed");

            rightGrip      = MakeMultiButton("RightGrip",
                "<XRController>{RightHand}/grip",
                "<XRController>{RightHand}/gripButton",
                "<OculusTouchController>{RightHand}/grip",
                "<OculusTouchController>{RightHand}/gripPressed");

            leftGrip       = MakeMultiButton("LeftGrip",
                "<XRController>{LeftHand}/grip",
                "<XRController>{LeftHand}/gripButton",
                "<OculusTouchController>{LeftHand}/grip",
                "<OculusTouchController>{LeftHand}/gripPressed");

            rightPrimary   = MakeMultiButton("RightPrimary",
                "<XRController>{RightHand}/primaryButton",
                "<OculusTouchController>{RightHand}/primaryButton");

            leftPrimary    = MakeMultiButton("LeftPrimary",
                "<XRController>{LeftHand}/primaryButton",
                "<OculusTouchController>{LeftHand}/primaryButton");

            rightSecondary = MakeMultiButton("RightSecondary",
                "<XRController>{RightHand}/secondaryButton",
                "<OculusTouchController>{RightHand}/secondaryButton");

            leftSecondary  = MakeMultiButton("LeftSecondary",
                "<XRController>{LeftHand}/secondaryButton",
                "<OculusTouchController>{LeftHand}/secondaryButton");

            menuButton     = MakeMultiButton("Menu",
                "<XRController>{LeftHand}/menuButton",
                "<OculusTouchController>{LeftHand}/menuButton");

            rightThumbstick = MakeAxis("RightThumbstick", "<XRController>{RightHand}/thumbstick");
            leftThumbstick  = MakeAxis("LeftThumbstick",  "<XRController>{LeftHand}/thumbstick");

            EnableAll();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            rightTrigger?.Dispose();   leftTrigger?.Dispose();
            rightGrip?.Dispose();      leftGrip?.Dispose();
            rightPrimary?.Dispose();   leftPrimary?.Dispose();
            rightSecondary?.Dispose(); leftSecondary?.Dispose();
            menuButton?.Dispose();
            rightThumbstick?.Dispose(); leftThumbstick?.Dispose();
        }

        // ── Factory helpers ────────────────────────────────────────────────────

        private static InputAction MakeMultiButton(string name, params string[] bindings)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings)
                a.AddBinding(b);
            return a;
        }

        private static InputAction MakeAxis(string name, string binding)
        {
            return new InputAction(name, InputActionType.Value,
                binding: binding,
                expectedControlType: "Vector2");
        }

        private void EnableAll()
        {
            rightTrigger.Enable();    leftTrigger.Enable();
            rightGrip.Enable();       leftGrip.Enable();
            rightPrimary.Enable();    leftPrimary.Enable();
            rightSecondary.Enable();  leftSecondary.Enable();
            menuButton.Enable();
            rightThumbstick.Enable(); leftThumbstick.Enable();
        }

        // ── Per-hand selector ──────────────────────────────────────────────────

        private InputAction Pick(InputAction left, InputAction right, bool isLeft)
            => isLeft ? left : right;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Index trigger held down.</summary>
        public bool GetTrigger(bool isLeft)
            => Pick(leftTrigger, rightTrigger, isLeft).IsPressed();

        /// <summary>Index trigger first pressed this frame.</summary>
        public bool GetTriggerDown(bool isLeft)
            => Pick(leftTrigger, rightTrigger, isLeft).WasPressedThisFrame();

        /// <summary>Either index trigger held down.</summary>
        public bool GetEitherTrigger() => GetTrigger(false) || GetTrigger(true);

        /// <summary>Grip (middle-finger) trigger held down.</summary>
        public bool GetGrip(bool isLeft)
            => Pick(leftGrip, rightGrip, isLeft).IsPressed();

        /// <summary>Either grip held down.</summary>
        public bool GetEitherGrip() => GetGrip(false) || GetGrip(true);

        /// <summary>Thumbstick axis value.</summary>
        public Vector2 GetThumbstick(bool isLeft)
            => Pick(leftThumbstick, rightThumbstick, isLeft).ReadValue<Vector2>();

        /// <summary>Sum of both thumbsticks' Y axes (used for scaling / power).</summary>
        public float GetBothThumbsticksY()
            => GetThumbstick(false).y + GetThumbstick(true).y;

        /// <summary>Primary face button (A = right hand, X = left hand) held.</summary>
        public bool GetPrimaryButton(bool isLeft)
            => Pick(leftPrimary, rightPrimary, isLeft).IsPressed();

        /// <summary>Primary face button first pressed this frame.</summary>
        public bool GetPrimaryButtonDown(bool isLeft)
            => Pick(leftPrimary, rightPrimary, isLeft).WasPressedThisFrame();

        /// <summary>Secondary face button (B = right, Y = left) held.</summary>
        public bool GetSecondaryButton(bool isLeft)
            => Pick(leftSecondary, rightSecondary, isLeft).IsPressed();

        /// <summary>Menu / Oculus button held (left controller).</summary>
        public bool GetMenuButton() => menuButton.IsPressed();

        /// <summary>Menu button first pressed this frame.</summary>
        public bool GetMenuButtonDown() => menuButton.WasPressedThisFrame();
    }
}
