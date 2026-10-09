using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace CampusLife.Runtime.Player
{
    /// <summary>
    /// Single place that reads devices and exposes per-frame intent. Supports
    /// the Input System package (keyboard, mouse, gamepad, touch) and, when a
    /// project uses the legacy Input Manager instead, the same controls through
    /// UnityEngine.Input. On-screen touch controls feed in through the
    /// <c>Touch*</c> members so gameplay code never touches devices directly.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        // Pixel to degree conversion for mouse look before user sensitivity.
        const float MouseDegreesPerPixel = 0.12f;
        const float TouchDegreesPerPixel = 0.18f;
        const float GamepadDegreesPerSecond = 160f;

        public Vector2 Move { get; private set; }
        /// <summary>Look delta in degrees this frame (x = yaw, y = pitch, mouse-up positive).</summary>
        public Vector2 Look { get; private set; }
        public float Zoom { get; private set; }
        public bool RunHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool DashboardPressed { get; private set; }
        public bool MapPressed { get; private set; }
        public bool ContinuePressed { get; private set; }
        /// <summary>0-based index of a dialogue choice number key pressed this frame, or -1.</summary>
        public int ChoicePressed { get; private set; } = -1;

        /// <summary>Overrides movement input; used by automated tests and scripted sequences.</summary>
        public Vector2? SimulatedMove { get; set; }

        /// <summary>When false, mouse movement does not rotate the camera (cursor visible for menus).</summary>
        public bool MouseLookEnabled { get; set; }

        // ---- On-screen controls ----
        public bool TouchControlsEnabled { get; set; }
        public bool TouchRunToggled { get; set; }
        /// <summary>Returns true when a screen point (bottom-left origin) is over an on-screen button.</summary>
        public System.Func<Vector2, bool> IsOverTouchControl { get; set; }
        /// <summary>Virtual joystick state in screen pixels (bottom-left origin), for drawing.</summary>
        public bool JoystickActive => _stickFingerId >= 0;
        public Vector2 JoystickCenter { get; private set; }
        public Vector2 JoystickKnob { get; private set; }
        public float JoystickRadius => Mathf.Max(60f, Screen.dpi > 0 ? Screen.dpi * 0.45f : Screen.height * 0.11f);

        bool _queuedInteract, _queuedPause, _queuedDashboard, _queuedMap;
        int _lookFingerId = -1;
        int _stickFingerId = -1;
        Vector2 _lastLookPosition;
        Vector2 _stickMove;

        public void QueueInteract() => _queuedInteract = true;
        public void QueuePause() => _queuedPause = true;
        public void QueueDashboard() => _queuedDashboard = true;
        public void QueueMap() => _queuedMap = true;

#if ENABLE_INPUT_SYSTEM
        void OnEnable() { EnhancedTouchSupport.Enable(); }
        void OnDisable() { EnhancedTouchSupport.Disable(); }
#endif

        public static bool TouchPreferred()
        {
#if ENABLE_INPUT_SYSTEM
            return Touchscreen.current != null && Mouse.current == null;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.touchSupported && !Input.mousePresent;
#else
            return false;
#endif
        }

        void Update()
        {
            Vector2 move = Vector2.zero, look = Vector2.zero;
            float zoom = 0f;
            bool run = false, interact = false, pause = false, dashboard = false, map = false, cont = false;
            int choice = -1;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                move.x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                move.y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                run = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                interact = kb.eKey.wasPressedThisFrame;
                pause = kb.escapeKey.wasPressedThisFrame;
                dashboard = kb.tabKey.wasPressedThisFrame;
                map = kb.mKey.wasPressedThisFrame;
                cont = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                if (kb.digit1Key.wasPressedThisFrame) choice = 0;
                else if (kb.digit2Key.wasPressedThisFrame) choice = 1;
                else if (kb.digit3Key.wasPressedThisFrame) choice = 2;
                else if (kb.digit4Key.wasPressedThisFrame) choice = 3;
                else if (kb.digit5Key.wasPressedThisFrame) choice = 4;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (MouseLookEnabled) look += mouse.delta.ReadValue() * MouseDegreesPerPixel;
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) zoom += Mathf.Sign(scroll);
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > move.sqrMagnitude) move = stick;
                look += pad.rightStick.ReadValue() * (GamepadDegreesPerSecond * Time.unscaledDeltaTime);
                run |= pad.leftStickButton.isPressed || pad.rightTrigger.ReadValue() > 0.5f;
                interact |= pad.buttonSouth.wasPressedThisFrame;
                cont |= pad.buttonSouth.wasPressedThisFrame;
                pause |= pad.startButton.wasPressedThisFrame;
                dashboard |= pad.selectButton.wasPressedThisFrame;
                map |= pad.buttonNorth.wasPressedThisFrame;
                if (pad.dpad.up.wasPressedThisFrame) zoom += 1f;
                if (pad.dpad.down.wasPressedThisFrame) zoom -= 1f;
            }
            if (TouchControlsEnabled)
            {
                foreach (var t in ETouch.activeTouches)
                    HandleTouch(t.finger.index, t.screenPosition, t.phase == UnityEngine.InputSystem.TouchPhase.Began,
                        t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled, ref look);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            move.x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            move.y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            interact = Input.GetKeyDown(KeyCode.E);
            pause = Input.GetKeyDown(KeyCode.Escape);
            dashboard = Input.GetKeyDown(KeyCode.Tab);
            map = Input.GetKeyDown(KeyCode.M);
            cont = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            for (int i = 0; i < 5; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { choice = i; break; }
            if (MouseLookEnabled && !TouchControlsEnabled)
                look += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * (10f * MouseDegreesPerPixel);
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f) zoom += Mathf.Sign(wheel);
            if (TouchControlsEnabled)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    HandleTouch(t.fingerId, t.position, t.phase == TouchPhase.Began,
                        t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled, ref look);
                }
            }
#endif
            if (!TouchControlsEnabled) ReleaseTouches();
            if (TouchControlsEnabled && _stickMove.sqrMagnitude > move.sqrMagnitude) move = _stickMove;
            run |= TouchControlsEnabled && TouchRunToggled;

            if (SimulatedMove.HasValue) move = SimulatedMove.Value;
            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            Zoom = zoom;
            RunHeld = run;
            InteractPressed = interact || _queuedInteract;
            PausePressed = pause || _queuedPause;
            DashboardPressed = dashboard || _queuedDashboard;
            MapPressed = map || _queuedMap;
            ContinuePressed = cont;
            ChoicePressed = choice;
            _queuedInteract = _queuedPause = _queuedDashboard = _queuedMap = false;
        }

        void ReleaseTouches()
        {
            _lookFingerId = -1;
            _stickFingerId = -1;
            _stickMove = Vector2.zero;
        }

        /// <summary>
        /// Left side of the screen: a floating joystick appears where the thumb lands.
        /// Right side: swiping rotates the camera. Touches that start on buttons are ignored.
        /// </summary>
        void HandleTouch(int fingerId, Vector2 position, bool began, bool ended, ref Vector2 look)
        {
            if (began)
            {
                if (IsOverTouchControl != null && IsOverTouchControl(position)) return;
                if (position.x < Screen.width * 0.4f && _stickFingerId < 0)
                {
                    _stickFingerId = fingerId;
                    JoystickCenter = JoystickKnob = position;
                    _stickMove = Vector2.zero;
                    return;
                }
                if (position.x >= Screen.width * 0.4f && _lookFingerId < 0)
                {
                    _lookFingerId = fingerId;
                    _lastLookPosition = position;
                    return;
                }
            }
            if (fingerId == _stickFingerId)
            {
                Vector2 offset = Vector2.ClampMagnitude(position - JoystickCenter, JoystickRadius);
                JoystickKnob = JoystickCenter + offset;
                _stickMove = offset / JoystickRadius;
                if (ended)
                {
                    _stickFingerId = -1;
                    _stickMove = Vector2.zero;
                }
            }
            else if (fingerId == _lookFingerId)
            {
                look += (position - _lastLookPosition) * TouchDegreesPerPixel;
                _lastLookPosition = position;
                if (ended) _lookFingerId = -1;
            }
        }
    }
}
