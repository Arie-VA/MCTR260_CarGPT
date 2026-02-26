using UnityEngine;

public class PathFollower
{
    public struct VelocityCommand
    {
        public float vx;    // Forward in robot body frame
        public float vy;    // Sideways in robot body frame
        public float omega; // Angular velocity (rad/s)
    }

    private HardwareConfig hardware;
    private ControlConfig control;

    private Vector2 currentLinearVelocity = Vector2.zero;
    private float currentAngularVelocity = 0f;

    public PathFollower(HardwareConfig hw, ControlConfig ctrl)
    {
        hardware = hw;
        control = ctrl;
    }

    public VelocityCommand ComputeCommand(
        Vector3 currentPosition,
        float currentYawRad,
        Vector3[] path,
        float finalTargetYawRad,
        float deltaTime)
    {
        VelocityCommand cmd = new VelocityCommand();

        if (path == null || path.Length == 0)
            return cmd; // No path, stop robot

        // --- 1️⃣ Get lookahead point ---
        Vector3 lookaheadTarget = GetLookaheadPoint(currentPosition, path);

        // --- 2️⃣ Position error relative to world ---
        Vector2 errorWorld = new Vector2(
            lookaheadTarget.x - currentPosition.x,
            lookaheadTarget.z - currentPosition.z
        );

        float distanceToGoal = new Vector2(
            path[path.Length - 1].x - currentPosition.x,
            path[path.Length - 1].z - currentPosition.z
        ).magnitude;

        // --- 3️⃣ Convert error to body frame ---
        float cos = Mathf.Cos(currentYawRad);
        float sin = Mathf.Sin(currentYawRad);
        float exBody = cos * errorWorld.x + sin * errorWorld.y;
        float eyBody = -sin * errorWorld.x + cos * errorWorld.y;

        // --- 4️⃣ Desired linear velocity ---
        Vector2 desiredLinearVelocity = new Vector2(
            control.positionGain * exBody,
            control.positionGain * eyBody
        );

        // Clamp to max linear speed
        if (desiredLinearVelocity.magnitude > hardware.MaxLinearSpeed)
            desiredLinearVelocity = desiredLinearVelocity.normalized * hardware.MaxLinearSpeed;

        // --- 5️⃣ Heading control ---
        float desiredYaw;
        if (distanceToGoal < control.goalOrientationThreshold)
        {
            // Near the goal → face directly toward it
            desiredYaw = Mathf.Atan2(errorWorld.y, errorWorld.x);
        }
        else
        {
            // Use final target yaw for intermediate segments
            desiredYaw = finalTargetYawRad;
        }

        float headingError = Mathf.DeltaAngle(currentYawRad * Mathf.Rad2Deg,
                                              desiredYaw * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        float desiredAngularVelocity = control.headingGain * headingError;
        desiredAngularVelocity = Mathf.Clamp(desiredAngularVelocity,
                                             -hardware.MaxAngularSpeed,
                                             hardware.MaxAngularSpeed);

        // --- 6️⃣ Linear acceleration limiting ---
        float maxLinearDelta = hardware.MaxLinearAcceleration * deltaTime;
        Vector2 linearDelta = desiredLinearVelocity - currentLinearVelocity;
        if (linearDelta.magnitude > maxLinearDelta)
            linearDelta = linearDelta.normalized * maxLinearDelta;
        currentLinearVelocity += linearDelta;

        // --- 7️⃣ Angular acceleration limiting ---
        float maxAngularDelta = hardware.MaxAngularAcceleration * deltaTime;
        float angularDelta = desiredAngularVelocity - currentAngularVelocity;
        angularDelta = Mathf.Clamp(angularDelta, -maxAngularDelta, maxAngularDelta);
        currentAngularVelocity += angularDelta;

        // --- 8️⃣ Output command ---
        cmd.vx = currentLinearVelocity.x;
        cmd.vy = currentLinearVelocity.y;
        cmd.omega = currentAngularVelocity;

        return cmd;
    }

    private Vector3 GetLookaheadPoint(Vector3 currentPosition, Vector3[] path)
    {
        float lookahead = control.lookaheadDistance;

        foreach (Vector3 p in path)
        {
            Vector2 diff = new Vector2(p.x - currentPosition.x, p.z - currentPosition.z);
            if (diff.magnitude > lookahead)
                return p;
        }

        // Default to last path point if none found
        return path[path.Length - 1];
    }
}