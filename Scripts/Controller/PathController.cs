using UnityEngine;
using System.Collections.Generic;

public class PathController : MonoBehaviour
{
    public RobotConfig controlConfig;
    public Rigidbody rb;

    private List<PathFollower.PathPoint> waypoints;
    private int  currentWaypointIndex = 0;
    private bool travelReversed       = false;

    private int cornersPassedCount = 0;
    public  int CornersPassed => cornersPassedCount;

    void Awake()
    {
        if (controlConfig == null)
            Debug.LogError("PathController: ControlConfig not assigned!");
    }

    public void SetWaypoints(List<PathFollower.PathPoint> newWaypoints)
    {
        waypoints            = newWaypoints;
        currentWaypointIndex = 1;
        cornersPassedCount   = 0;
    }

    public void SetReversed(bool reversed) => travelReversed = reversed;

    // ─────────────────────────────────────────────────────────────────────
    // Returns vx/vy normalized [-1, 1] — multiply by max speed downstream.
    // ─────────────────────────────────────────────────────────────────────
    public VelocityOutput ComputeVelocity(Vector3 currentPosition, Quaternion currentRotation)
    {
        if (waypoints == null || waypoints.Count < 2)
            return new VelocityOutput { vx = 0, vy = 0, omega = 0 };

        // ── Advance waypoint ───────────────────────────────────────────
        while (currentWaypointIndex < waypoints.Count - 1)
        {
            PathFollower.PathPoint wp = waypoints[currentWaypointIndex];
            Vector3 pathDir = new Vector3(
                Mathf.Cos(wp.targetHeading), 0f, Mathf.Sin(wp.targetHeading));
            Vector3 toRobot = currentPosition - wp.position;
            

            if (Vector3.Dot(toRobot, pathDir) >= 0f)
            {
                if (waypoints[currentWaypointIndex].isCorner)
                    cornersPassedCount++;
                currentWaypointIndex++;
            }
            else
                break;
        }

        PathFollower.PathPoint target = waypoints[currentWaypointIndex];

        // ── Distance Check ─────────────────────────────────────────────
        Vector3 toTarget   = target.position - currentPosition;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float   dist       = toTargetXZ.magnitude;

        if (dist < 0.001f)
        {
            if (currentWaypointIndex < waypoints.Count - 1)
                currentWaypointIndex++;
            return new VelocityOutput { vx = 0, vy = 0, omega = 0 };
        }

        Vector3 dirWorld = Vector3.zero;

        // ── Translation & Cross-track correction ───────────────────────
        if (currentWaypointIndex > 0)
        {
            PathFollower.PathPoint prev = waypoints[currentWaypointIndex - 1];
            Vector3 segDir = new Vector3(
                Mathf.Cos(target.targetHeading), 0f, Mathf.Sin(target.targetHeading));
            
            // 1. Base direction is strictly parallel to the path segment
            dirWorld = segDir; 

            // 2. Calculate cross-track error
            Vector3 toRobotFromPrev = currentPosition - prev.position;
            Vector3 segLeft    = new Vector3(-segDir.z, 0f, segDir.x);
            float   crossTrack = Vector3.Dot(toRobotFromPrev, segLeft);

            // 3. Apply perpendicular correction to get back on the line
            Vector3 correctionWorld = -segLeft * crossTrack * controlConfig.crossTrackGain;
            dirWorld += correctionWorld;
            
            // Normalize so the combined vectors don't exceed max speed
            dirWorld.Normalize();
        }
        else
        {
            // Fallback if there is no previous waypoint to form a segment
            dirWorld = toTargetXZ / dist;
        }

        Vector3 axisX = currentRotation * Vector3.right;
        Vector3 axisZ = currentRotation * Vector3.forward;

        float vx = Vector3.Dot(dirWorld, axisX); 
        float vy = Vector3.Dot(dirWorld, axisZ); 

        // Clamp just in case floating point inaccuracies push it slightly out of bounds
        vx = Mathf.Clamp(vx, -1f, 1f);
        vy = Mathf.Clamp(vy, -1f, 1f);

        // ── Heading control ────────────────────────────────────────────
        // Removed lookahead index — simply align to the current target's heading.
        float desiredHeading = target.targetHeading;
        if (travelReversed) desiredHeading += Mathf.PI;

        float currentHeading = GetHeading(currentRotation);

        float headingErrorDeg = Mathf.DeltaAngle(
            currentHeading * Mathf.Rad2Deg,
            desiredHeading * Mathf.Rad2Deg
        );

        float deadband = 2f;
        float absError = Mathf.Abs(headingErrorDeg);
        float omega    = 0f;

        if (absError > deadband)
        {
            float effectiveGain = controlConfig.headingGain * Mathf.Clamp01(absError / 15f);
            omega = Mathf.Clamp(
                effectiveGain * headingErrorDeg * Mathf.Deg2Rad,
                -controlConfig.maxRotationSpeed,
                controlConfig.maxRotationSpeed
            );
        }

        return new VelocityOutput { vx = vx, vy = vy, omega = omega };
    }

    // ─────────────────────────────────────────────────────────────────────
    public bool HasReachedDestination()
    {
        if (waypoints == null || waypoints.Count == 0) return false;
        return currentWaypointIndex >= waypoints.Count - 1;
        Debug.Log("destination reached");
    }

    private float GetHeading(Quaternion rotation)
    {
        Vector3 fwd = rotation * Vector3.right;
        return Mathf.Atan2(fwd.z, fwd.x);
    }
}