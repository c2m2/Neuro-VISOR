using UnityEngine;
using UnityEngine.InputSystem;

namespace C2M2.Interaction.VR
{
    /// <summary>
    /// Singleton bridge from the legacy OVRInput API to the Unity Input System.
    /// Provides equivalent functionality for the controller buttons and axes used
    /// throughout this project. Lazily self-creates when first accessed during play.
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

            rightTrigger    = MakeButton("RightTrigger",    "{RightHand}/trigger");
            leftTrigger     = MakeButton("LeftTrigger",     "{LeftHand}/trigger");
            rightGrip       = MakeButton("RightGrip",       "{RightHand}/grip");
            leftGrip        = MakeButton("LeftGrip",        "{LeftHand}/grip");
            rightPrimary    = MakeButton("RightPrimary",    "{RightHand}/primaryButton");
            leftPrimary     = MakeButton("LeftPrimary",     "{LeftHand}/primaryButton");
            rightSecondary  = MakeButton("RightSecondary",  "{RightHand}/secondaryButton");
            leftSecondary   = MakeButton("LeftSecondary",   "{LeftHand}/secondaryButton");
            menuButton      = MakeButton("Menu",            "{LeftHand}/menuButton");

            rightThumbstick = MakeAxis("RightThumbstick",   "{RightHand}/thumbstick");
            leftThumbstick  = MakeAxis("LeftThumbstick",    "{LeftHand}/thumbstick");

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

        private static InputAction MakeButton(string name, string hand)
        {
            var a = new InputAction(name, InputActionType.Button,
                binding: "<XRController>" + hand);
            return a;
        }

        private static InputAction MakeAxis(string name, string hand)
        {
            var a = new InputAction(name, InputActionType.Value,
                binding: "<XRController>" + hand,
                expectedControlType: "Vector2");
            return a;
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
