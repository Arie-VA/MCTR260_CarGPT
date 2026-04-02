using UnityEngine;
using UnityEngine.AI;

public class PathPlanner : MonoBehaviour
{
    [Header("Visualization")]
    public Color pathColour = Color.red;

    [Header("NavMesh Agent")]
    [Tooltip("agentTypeID of your baked agent.")]
    public int navMeshAgentTypeID = 0;

    [Header("Corner Clearance")]
    [Tooltip("Distance before and after each corner to insert clearance waypoints (metres). " +
             "Set to roughly your robot's half-width + small margin.")]
    public float cornerClearance = 0.2f;

    [Tooltip("Only insert clearance waypoints at corners sharper than this (degrees).")]
    public float minBendAngle = 20f;

    [Tooltip("NavMesh corners closer than this distance are merged into one before processing.")]
    public float mergeDistance = 0.15f;

    private NavMeshPath navMeshPath;
    private Vector3[] currentPath = new Vector3[0];

    void Awake()
    {
        navMeshPath = new NavMeshPath();
    }

    public Vector3[] GeneratePath(Vector3 startPos, Vector3 targetPos)
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshAgentTypeID,
            areaMask    = NavMesh.AllAreas
        };

        bool success = NavMesh.CalculatePath(startPos, targetPos, filter, navMeshPath);

        if (success && navMeshPath.status == NavMeshPathStatus.PathComplete)
            currentPath = InsertCornerWaypoints(navMeshPath.corners);
        else
            currentPath = new Vector3[0];

        return currentPath;
    }

    public Vector3[] GetCurrentPath() => currentPath;

    // ── Merge corners that are too close together ─────────────────────────
    // NavMesh sometimes generates two nearly-identical points at a wall junction.
    // These collapse the clearance clamp to near-zero, making offsetting useless.
    // Merging them first gives the clearance math room to work.
    private Vector3[] MergeCloseCorners(Vector3[] corners)
    {
        var merged = new System.Collections.Generic.List<Vector3>();
        merged.Add(corners[0]);

        for (int i = 1; i < corners.Length; i++)
        {
            if (Vector3.Distance(corners[i], merged[merged.Count - 1]) > mergeDistance)
                merged.Add(corners[i]);
        }

        return merged.ToArray();
    }

    // ── Corner waypoint insertion ─────────────────────────────────────────
    private Vector3[] InsertCornerWaypoints(Vector3[] corners)
    {
        corners = MergeCloseCorners(corners);

        if (corners.Length < 3)
            return corners;

        for (int i = 0; i < corners.Length; i++)
            Debug.Log($"Merged corner {i}: {corners[i]}");

        var result = new System.Collections.Generic.List<Vector3>();
        result.Add(corners[0]);

        for (int i = 1; i < corners.Length - 1; i++)
        {
            Vector3 prev = corners[i - 1];
            Vector3 curr = corners[i];
            Vector3 next = corners[i + 1];

            Vector3 dirIn  = new Vector3(curr.x - prev.x, 0f, curr.z - prev.z).normalized;
            Vector3 dirOut = new Vector3(next.x - curr.x, 0f, next.z - curr.z).normalized;

            float bendAngle = Vector3.Angle(dirIn, dirOut);

            if (bendAngle < minBendAngle)
            {
                result.Add(curr);
                continue;
            }

            float distIn  = Vector3.Distance(prev, curr);
            float distOut = Vector3.Distance(curr, next);
            float c       = Mathf.Min(cornerClearance, distIn * 0.45f, distOut * 0.45f);

            Vector3 p1 = curr - dirIn  * c;
            Vector3 p2 = curr + dirOut * c;

            result.Add(p1);
            result.Add(curr);
            result.Add(p2);
        }

        result.Add(corners[corners.Length - 1]);
        return result.ToArray();
    }

    void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Length < 2) return;

        Gizmos.color = pathColour;
        for (int i = 0; i < currentPath.Length - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);

        Gizmos.DrawSphere(currentPath[currentPath.Length - 1], 0.05f);
    }
}