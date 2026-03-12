using UnityEngine;
using System.Collections.Generic;

public class WarehouseAIController : MonoBehaviour
{
    [Header("References")]
    public AreaSelector areaSelector;
    public PathPlanner pathPlanner;
    public ControlConfig controlConfig;
    public PathController pathController;
    public Transform dropoffZone1;
    public Transform dropoffZone2;
    public Transform dropoffZone3;

    [Header("Temp Movement Controller")]
    public AIMovementController movementController;
    private Rigidbody rb;

    // --- State Bools ---
    private bool hasObjects = false;
    private bool hasTarget  = false;
    private bool atTarget   = false;

    // --- Shared Data ---
    private Vector3[] currentPath = new Vector3[0];
    private int currentTargetZone = 0;
    private PathFollower pathFollower;
    private List<PathFollower.PathPoint> currentWaypoints;
    private bool pathLoaded = false;

    // ─────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (controlConfig == null)
        {
            Debug.LogError("WarehouseAIController: ControlConfig not assigned!");
            return;
        }
        if (movementController == null)
        {
            Debug.LogError("WarehouseAIController: AIMovementController not assigned!");
            return;
        }

        pathFollower = new PathFollower(controlConfig);
        pathFollower.Initialize();
        rb.sleepThreshold = 0f;
    }

    // ─────────────────────────────────────────────
    void Update()
    {
        if (!hasTarget && !atTarget) Pathing();
        else if (hasTarget && !atTarget) Moving2Target();
        else if (!hasObjects && atTarget) Pickup();
        else if (hasObjects && atTarget) Dropoff();
    }

    // ─────────────────────────────────────────────
    private void Pathing()
    {
        Vector3 destination;

        if (!hasObjects)
        {
            var result = areaSelector.GetBestTargetArea();
            if (result == null)
            {
                Debug.Log("Pathing: No valid pickup targets found. Staying idle.");
                return;
            }

            destination = result.Value.centroid;
            currentTargetZone = result.Value.zoneDesignation;
        }
        else
        {
            Transform dropoff = currentTargetZone switch
            {
                1 => dropoffZone1,
                2 => dropoffZone2,
                3 => dropoffZone3,
                _ => null
            };

            if (dropoff == null)
            {
                Debug.LogError($"Pathing: No dropoff zone assigned for zone {currentTargetZone}.");
                return;
            }

            destination = dropoff.position;
        }

        Vector3[] path = pathPlanner.GeneratePath(transform.position, destination);
        for (int i = 0; i < path.Length; i++)
            Debug.Log($"Corner {i}: {path[i]}");

        if (path.Length == 0)
        {
            Debug.LogWarning($"Pathing: PathPlanner returned empty path to {destination}.");
            return;
        }

        currentPath = path;
        hasTarget   = true;
        pathLoaded  = false;
        Debug.LogWarning("Pathing complete - SHOULD HAPPEN ONCE");
    }

    // ─────────────────────────────────────────────
    private void Moving2Target()
    {
        if (pathFollower == null || currentPath.Length == 0)
        {
            Debug.LogWarning("Moving2Target: PathFollower not initialized or no path available.");
            return;
        }

        // Load path into PathFollower once per path (not every frame)
        if (!pathLoaded)
        {
            currentWaypoints = pathFollower.SetPath(currentPath);
            pathController.SetWaypoints(currentWaypoints);
            pathLoaded = true;
            Debug.LogWarning($"Loaded path with {currentWaypoints.Count} waypoints.");
            
        }

        // Check if reached destination
        if (pathController.HasReachedDestination())
        {
            Debug.LogWarning("Destination reached, changing states");
            atTarget   = true;
            hasTarget  = false;
            pathLoaded = false;
            VelocityOutput velocity = new VelocityOutput {vx = 0, vy = 0, omega = 0};
            movementController.ApplyVelocity(velocity);
            return;
        } else {
            // Compute and apply velocity this frame
            VelocityOutput velocity = pathController.ComputeVelocity(transform.position, transform.rotation);
            movementController.ApplyVelocity(velocity);
        }
        

        
    }

    // ─────────────────────────────────────────────
    private void Pickup()
    {
        // Enable PPO pickup model
        // On pickup complete: set hasObjects = true, atTarget = false
    }

    private void Dropoff()
    {
        // Run Puke.exe
        // On complete: set hasObjects = false, hasTarget = false, atTarget = false
    }
}