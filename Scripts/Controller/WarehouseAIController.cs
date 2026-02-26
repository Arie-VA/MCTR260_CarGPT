using UnityEngine;

public class WarehouseAIController : MonoBehaviour
{
    [Header("References")]
    public AreaSelector areaSelector;
    public PathPlanner pathPlanner;
    public PathFollower pathFollower;

    // --- State Bools ---
    private bool hasObjects = false;
    private bool hasTarget  = false;
    private bool atTarget   = false;

    // --- Shared Data ---
    private Vector3[] currentPath = new Vector3[0];

    // ─────────────────────────────────────────────
    void Update()
    {
        if      (!hasTarget && !atTarget)  Pathing();
        else if ( hasTarget && !atTarget)  Moving2Target();
        else if (!hasObjects && atTarget)  Pickup();
        else if ( hasObjects && atTarget)  Dropoff();
    }

    // ─────────────────────────────────────────────
    private void Pathing()
    {
        // If no objects held, ask AreaSelector for pickup coordinates
        // If objects held, use DetectedObjects to get the dropoff coordinates
        // Either way, send coordinates to PathPlanner and store the returned path
        // Set hasTarget = true
    }

    private void Moving2Target()
    {
        // Run PathFollower on currentPath
        // Export Vx, Vy, omega to Pi communication layer
        // If within threshold distance to target: set atTarget = true, stop motors
    }

    private void Pickup()
    {
        // Enable PPO pickup model
        // On pickup complete: set hasObjects = true, atTarget = false
    }

    private void Dropoff()
    {
        // Run Puke.exe (dropoff algorithm)
        // On complete: set hasObjects = false, hasTarget = false, atTarget = false
    }
}