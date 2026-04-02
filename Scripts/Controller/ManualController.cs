using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the robot GameObject alongside WarehouseAIController.
// Requires the Unity Input System package.
//
// Options button toggles manual override on/off.
// Left stick        → vx (strafe) / vy (forward/back)
// Right stick X     → omega (rotation)
// Left bumper (L1)  → stepper +100 (raise / pickup direction)
// Right bumper (R1) → stepper -100 (lower / dropoff direction)
// Neither bumper    → stepper 0

[RequireComponent(typeof(AIMovementController))]
public class ManualController : MonoBehaviour
{
    [Header("Config")]
    public RobotConfig config;

    [Header("References")]
    public WarehouseAIController warehouseAI;
    public RobotUDPSender        udpSender;

    public bool IsOverrideActive { get; private set; } = false;

    private AIMovementController _movement;
    private Gamepad              _pad;
    private bool                 _optionsPrev;

    void Awake()
    {
        _movement = GetComponent<AIMovementController>();
    }

    void Update()
    {
        if (_pad == null)
        {
            _pad = Gamepad.current;
            if (_pad == null) return;
        }

        HandleToggle();

        if (IsOverrideActive)
            DriveFromSticks();
    }

    // ── Toggle (Options button, rising-edge only) ──────────────────────────
    private void HandleToggle()
    {
        bool optionsNow = _pad.startButton.isPressed;

        if (optionsNow && !_optionsPrev)
        {
            IsOverrideActive = !IsOverrideActive;

            if (!IsOverrideActive)
            {
                VelocityOutput stop = new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 };
                _movement.ApplyVelocity(stop);
                udpSender?.Send(stop, "Stop from ManualController HandleToggle");
            }

            Debug.Log($"[ManualController] Override {(IsOverrideActive ? "ON" : "OFF")}");
        }

        _optionsPrev = optionsNow;
    }

    // ── Read sticks + bumpers, push velocity ──────────────────────────────
    private void DriveFromSticks()
    {
        Vector2 leftStick  = _pad.leftStick.ReadValue();
        Vector2 rightStick = _pad.rightStick.ReadValue();

        // Deadzones
        if (leftStick.magnitude      < config.linearDeadzone)  leftStick    = Vector2.zero;
        if (Mathf.Abs(rightStick.x)  < config.angularDeadzone) rightStick.x = 0f;

        // Bumpers → stepper
        float stepper = 0f;
        if      (_pad.leftShoulder.isPressed)  stepper =  100f;  // L1 = raise
        else if (_pad.rightShoulder.isPressed) stepper = -100f;  // R1 = lower

        VelocityOutput velocity = new VelocityOutput
        {
            vy      =  -leftStick.x  * config.manualMaxLinearSpeed,
            vx      =   leftStick.y  * config.manualMaxLinearSpeed,
            omega   =  -rightStick.x * config.manualMaxAngularSpeed,
            stepper =   stepper
        };

        _movement.ApplyVelocity(velocity);
        udpSender?.Send(velocity, "ManualController DriveFromSticks");
    }
}