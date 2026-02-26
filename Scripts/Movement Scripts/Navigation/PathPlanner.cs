using UnityEngine;
using UnityEngine.AI; // This is needed for the actual path planning done via Navmesh

public class PathPlanner : MonoBehaviour
{ 
    [Header("Target and visualization")]
    public Color pathColour = Color.red; // The colour of the path


    private NavMeshPath navMeshPath; // Reusable NavMeshPath object
    private Vector3[] currentPath = new Vector3[0]; // The current path to the target


    void Awake()
    {
        navMeshPath = new NavMeshPath(); // Initialize the NavMeshPath object 
    }

    // Calculates path from current position to target and stores it in path2Target
    public Vector3[] GeneratePath(Vector3 startPos, Vector3 targetPos)
    {
        bool success = NavMesh.CalculatePath(
            startPos,
            targetPos,
            NavMesh.AllAreas,
            navMeshPath
        );

        if (success && navMeshPath.status == NavMeshPathStatus.PathComplete)
        {
            currentPath = navMeshPath.corners;
        }
        else
        {
            currentPath = new Vector3[0];
        }

        return currentPath;

    }

    public Vector3[] GetCurrentPath()
    {
        return currentPath;
    }

    //Drawing path via gizmos for visualization in the editor
    void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Length < 2)
            return;

        Gizmos.color = pathColour;

        for (int i = 0; i < currentPath.Length - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
        }

        Gizmos.DrawSphere(currentPath[currentPath.Length - 1], 0.05f);
    }
}
