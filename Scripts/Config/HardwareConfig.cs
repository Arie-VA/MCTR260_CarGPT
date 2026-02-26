using UnityEngine;

[System.Serializable]
// This calss holds all the hard numbers incase any data needs to be changed later
public class HardwareConfig
{
    [Header("Wheel Diameter(in) + Motor Max RPM")]
    public float wheelDiameterInches = 3.5f;
    public float motorMaxRPM = 1000f;

    [Header("Robot Dimensions (m)")]
    public float robotLength = 0.3f;
    public float robotWidth = 0.3f;

    [Header("usable Speed Range")]
    [Range(0.1f, 1.0f)]
    public float usableMaxSpeed = 1.0f;

    [Header("Acceloration Scaling")]
    public float accelTimeToMaxSpeed = 0.5f;


    // Below is Derived Values
    public float WheelRadiusMeters =>
        (wheelDiameterInches * 0.0254f) / 2f;

    public float MotorMaxRadPerSec =>
        motorMaxRPM * 2f * Mathf.PI / 60f;

    public float MaxLinearSpeed =>
        MotorMaxRadPerSec * WheelRadiusMeters * usableMaxSpeed;

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