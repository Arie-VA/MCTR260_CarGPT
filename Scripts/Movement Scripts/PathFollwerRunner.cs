using UnityEngine;

public class PathFollowerRunner : MonoBehaviour
{
    public PathPlanner pathPlanner;  // Assign your cube with PathPlanner here
    public Transform cube;           // The cube to follow
    public HardwareConfig hardware;  // Your hardware config
    public ControlConfig control;    // Your control config

    private PathFollower pathFollower;

    void Start()
    {
        pathFollower = new PathFollower(hardware, control);
    }

    void Update()
    {
        if (pathPlanner == null || cube == null) return;

        // Get current path
        Vector3[] path = pathPlanner.path2Target;

        // Call ComputeCommand
        var cmd = pathFollower.ComputeCommand(
            cube.position,
            cube.eulerAngles.y * Mathf.Deg2Rad,
            path,
            0f,           // final target yaw for now
            Time.deltaTime
        );

        // Debug output
        Debug.Log($"vx: {cmd.vx:F2}, vy: {cmd.vy:F2}, omega: {cmd.omega:F2}");
    }
}