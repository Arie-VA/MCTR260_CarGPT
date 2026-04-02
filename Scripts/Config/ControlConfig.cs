using UnityEngine;

// Create via: Assets → Create → Robot → RobotConfig
// One asset, drag into every script. Change here, everything updates.
[CreateAssetMenu(fileName = "RobotConfig", menuName = "Robot/RobotConfig")]
public class RobotConfig : ScriptableObject
{
    [Header("Wheel Configuration")]
    public float wheelDiameterInches = 3.0f;
    public float motorMaxRPM         = 1000f;
    public float MaxSpeed = 0.8f; // Cap max speed in any direction

    [Header("Robot Dimensions (m)")]
    public float robotLength = 0.3f;
    public float robotWidth  = 0.3f;

    [Header("Acceleration")]
    public float accelTimeToMaxSpeed = 0.5f;

    [Header("Pathing")]
    public float waypointSpacing       = 0.2f;
    public float waypointThreshold     = 0.2f;
    public float maxRotationSpeed      = 5.0f;
    public float maxPathDeviation      = 1.0f;
    public int   lookAheadIndex        = 3;

    [Header("Approach")]
    public float finalApproachDistance = 1.0f;

    [Header("PathController Gains")]
    public float positionGain   = 1.0f;
    public float headingGain    = 1.0f;
    public float crossTrackGain = 1.0f;

    [Header("Manual Control")]
    [Tooltip("Max vx/vy magnitude for manual stick drive (-1 to 1)")]
    public float manualMaxLinearSpeed  = 1.0f;
    [Tooltip("Max omega for manual stick drive")]
    public float manualMaxAngularSpeed = 1.5f;
    [Tooltip("Ignore linear stick input below this magnitude")]
    public float linearDeadzone        = 0.1f;
    [Tooltip("Ignore angular stick input below this magnitude")]
    public float angularDeadzone       = 0.1f;

    [Header("Pickup Stepper")]
    public float pickupRotations = 2.25f;
    public float pickupRPM       = 50f;

    [Header("Dropoff Stepper")]
    public float dropoffRotations = 3f;
    public float dropoffRPM       = 50f;

    // ─────────────────────────────────────────────
    // Derived Values
    public float WheelRadiusMeters      => (wheelDiameterInches * 0.0254f) / 2f;
    public float MotorMaxRadPerSec      => motorMaxRPM * 2f * Mathf.PI / 60f;
    public float MaxLinearSpeed         => MotorMaxRadPerSec * WheelRadiusMeters;
    public float RobotRadius            => Mathf.Sqrt(Mathf.Pow(robotWidth / 2f, 2f) + Mathf.Pow(robotLength / 2f, 2f));
    public float MaxAngularSpeed        => MaxLinearSpeed / RobotRadius;
    public float MaxLinearAcceleration  => MaxLinearSpeed / accelTimeToMaxSpeed;
    public float MaxAngularAcceleration => MaxAngularSpeed / accelTimeToMaxSpeed;

    // Stepper timing helpers
    public float PickupDuration  => pickupRotations  / (pickupRPM  / 60f);
    public float DropoffDuration => dropoffRotations / (dropoffRPM / 60f);
}