using UnityEngine;

[System.Serializable]
public class ControlConfig
{
    [Header("Wheel Configuration")]
    public float wheelDiameterInches = 3.0f;
    public float motorMaxRPM = 1000f;

    [Header("Robot Dimensions (m)")]
    public float robotLength = 0.3f;
    public float robotWidth = 0.3f;

    [Header("Usable Speed Range")]
    [Range(0.1f, 1.0f)]
    public float usableMaxSpeed = 0.1f; // precent between 0 and 1

    [Header("Acceleration Scaling")]
    public float accelTimeToMaxSpeed = 0.5f;

    [Header("Pathing")]
    public float waypointSpacing = 0.2f;
    public float waypointThreshold = 0.2f;
    public float maxRotationSpeed = 5.0f;
    public float maxPathDeviation = 1.0f;
    public int lookAheadIndex = 3; 

    [Header("Approach")]
    public float finalApproachDistance = 1.0f; // Distance from final corner to begin slowing down

    [Header("PathController Gains")]
    public float positionGain = 1.0f;  // For lateral correction (vy)
    public float headingGain = 1.0f;   // For angular correction (omega)

    // ─────────────────────────────────────────────
    // Derived Values (auto-calculated)
    public float WheelRadiusMeters =>
        (wheelDiameterInches * 0.0254f) / 2f;

    public float MotorMaxRadPerSec =>
        motorMaxRPM * 2f * Mathf.PI / 60f;

    public float MaxLinearSpeed =>
        MotorMaxRadPerSec * WheelRadiusMeters;

    public float RobotRadius =>
        Mathf.Sqrt(
            Mathf.Pow(robotWidth / 2f, 2f) +
            Mathf.Pow(robotLength / 2f, 2f)
        );

    public float MaxAngularSpeed =>
        MaxLinearSpeed / RobotRadius;

    public float MaxLinearAcceleration =>
        MaxLinearSpeed / accelTimeToMaxSpeed;

    public float MaxAngularAcceleration =>
        MaxAngularSpeed / accelTimeToMaxSpeed;
}