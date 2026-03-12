using UnityEngine;
using System.Collections.Generic;

public class PathController : MonoBehaviour
{
    public ControlConfig controlConfig;
    public Rigidbody rb;

    // --- Internal State ---
    private List<PathFollower.PathPoint> waypoints;
    private int currentWaypointIndex = 0;
    
    
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (controlConfig == null)
        {
            Debug.LogError("PathController: ControlConfig not assigned!");
        }
    }

    // ─────────────────────────────────────────────
    public void SetWaypoints(List<PathFollower.PathPoint> newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 1; // Index 0 is always the robot's current position, skip it
    }

    // ─────────────────────────────────────────────
    public VelocityOutput ComputeVelocity(Vector3 currentPosition, Quaternion currentRotation)
    {
    if (waypoints == null || waypoints.Count == 0)
    {
        return new VelocityOutput { vx = 0, vy = 0, omega = 0 };
    }

    if (waypoints.Count < 2)
    {
        Debug.LogWarning("PathController: Waypoint list has fewer than 2 points, cannot compute segment.");
        return new VelocityOutput { vx = 0, vy = 0, omega = 0 };
    }

    // Advance waypoint if robot has passed it
    while (currentWaypointIndex < waypoints.Count - 1)
    {
        PathFollower.PathPoint wp = waypoints[currentWaypointIndex];
        Vector3 toRobot = currentPosition - wp.position;
        float pathAngleCurrent = wp.targetHeading;
        Vector3 pathDirCurrent = new Vector3(Mathf.Cos(pathAngleCurrent), 0f, Mathf.Sin(pathAngleCurrent));

        if (Vector3.Dot(toRobot, pathDirCurrent) >= 0f)
            currentWaypointIndex++;
        else
            break;
    }

    PathFollower.PathPoint targetWaypoint = waypoints[currentWaypointIndex];

    // --- Compute world-space direction to waypoint ---
    Vector3 toTarget = targetWaypoint.position - currentPosition;
    Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
    float distToTarget = toTargetXZ.magnitude;

    if (distToTarget < 0.001f)
    {
        if (currentWaypointIndex < waypoints.Count - 1)
            currentWaypointIndex++;
        return new VelocityOutput { vx = 0, vy = 0, omega = 0 };
    }

    Vector3 dirWorld = toTargetXZ / distToTarget; // Unit vector pointing to target
    float speed = controlConfig.MaxLinearSpeed;

    Vector3 worldVel3D = dirWorld * speed;

    // --- Project into robot-local frame ---
    Vector3 robotVxAxis = currentRotation * Vector3.right;   // robot forward → vx
    Vector3 robotVyAxis = currentRotation * Vector3.forward; // robot lateral → vy
    

    float vx = Vector3.Dot(worldVel3D, robotVxAxis);
    float vy = Vector3.Dot(worldVel3D, robotVyAxis);

    // --- Heading control: steer toward the target waypoint's desired heading ---
    int lookAheadIndex = controlConfig.lookAheadIndex + currentWaypointIndex;
    if (currentWaypointIndex >= waypoints.Count - (1+controlConfig.lookAheadIndex))
        lookAheadIndex = currentWaypointIndex;
    PathFollower.PathPoint lookAheadWaypoint = waypoints[lookAheadIndex];

    float currentHeading = GetHeadingFromRotation(currentRotation);
    float headingError = Mathf.DeltaAngle(
        currentHeading * Mathf.Rad2Deg,
        lookAheadWaypoint.desiredOmega * Mathf.Rad2Deg
    ) * Mathf.Deg2Rad;

    float omega = Mathf.Clamp(
        -controlConfig.headingGain * headingError,
        -controlConfig.maxRotationSpeed,
        controlConfig.maxRotationSpeed
    );

    
    Debug.Log($"desiredOmega: {targetWaypoint.desiredOmega} currentHeading: {currentHeading} ");
    return new VelocityOutput { vx = vx, vy = vy, omega = omega };
    }


    // ─────────────────────────────────────────────
    public bool HasReachedDestination()
    {
        if (waypoints == null || waypoints.Count == 0)
            return false;

        return currentWaypointIndex >= waypoints.Count - 1;
    }

    // ─────────────────────────────────────────────
    /// Helper: Convert Quaternion rotation to heading angle in radians
    private float GetHeadingFromRotation(Quaternion rotation)
    {
        // Robot forward is +X axis
        Vector3 forward = rotation * Vector3.right;
        float heading = Mathf.Atan2(forward.z, forward.x);
        return heading;
    }
}