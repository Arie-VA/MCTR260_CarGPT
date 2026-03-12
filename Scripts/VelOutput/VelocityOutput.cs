using UnityEngine;

/// <summary>
/// Simple data container for velocity commands to be sent to the Pi.
/// Another script can subscribe to this or poll GetLatestVelocityOutput() from WarehouseAIController
/// to send commands via Bluetooth to the physical robot.
/// </summary>
public class VelocityOutput
{
    public float vx;        // Forward velocity in robot body frame (m/s)
    public float vy;        // Sideways velocity in robot body frame (m/s)
    public float omega;     // Angular velocity (rad/s)
    public float timestamp; // Time when this command was generated

    public VelocityOutput()
    {
        vx = 0f;
        vy = 0f;
        omega = 0f;
        timestamp = 0f;
    }

    public override string ToString()
    {
        return $"[Vx: {vx:F3}, Vy: {vy:F3}, Omega: {omega:F3}]";
    }
}
