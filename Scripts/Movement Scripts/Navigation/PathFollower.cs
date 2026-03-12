using UnityEngine;
using System.Collections.Generic;

// Omega lead path follower
// Creates several points between each corner gotten from PathPlanner
// Each point stores the omega chosen for any given moment
// corners will be fully facing vx in the direction we want to go. 
// vy and vx will be chosen for each point that run a max val in Control Config

public class PathFollower
{
    // Configuration
    private ControlConfig ctrlFig;

    private float maxLinearVel;
    private float maxOmega;
    private float pointSpacing;
    private Vector3[] corners;
    private List<PathPoint> waypoints;

    public PathFollower(ControlConfig control)
    {
        ctrlFig = control;
    }
    
    public void Initialize() {
        maxLinearVel = ctrlFig.MaxLinearSpeed;
        maxOmega = ctrlFig.MaxAngularSpeed;
        pointSpacing = ctrlFig.waypointSpacing;
    }

    public struct PathPoint
    {
        public Vector3 position;
        public float desiredOmega; // Heading of next segment
        public float targetHeading; // Direction to next corner
        public bool isCorner;
    }

    public List<PathPoint> SetPath(Vector3[] newPath) {
        corners = newPath;

        // Assign my library to waypoints
        waypoints = BuildWayPoints(corners, pointSpacing);

        // Append desired omega to each waypoint
        waypoints = AppendDesiredOmega(waypoints);

        return waypoints;
    }

    // Create empty Library of waypoints
    public List<PathPoint> BuildWayPoints(Vector3[] corners, float pointSpacing) {
        List<PathPoint> waypoints = new List<PathPoint>(); // Create empty list

        // for loop to place many waypoints between corners
        for (int i = 0; i < corners.Length - 1; i++) {
            
            Vector3 lastPoint = corners[i];
            Vector3 nextPoint = corners[i+1];
            float segLen = Vector3.Distance(lastPoint, nextPoint);
            int waypointCount = Mathf.Max(2, Mathf.RoundToInt(segLen / pointSpacing));
            float heading = Mathf.Atan2(nextPoint.z - lastPoint.z, nextPoint.x - lastPoint.x);


            // always add corner at begining of loop. 
            waypoints.Add(new PathPoint { position = corners[i], targetHeading = heading, isCorner = true});

            // interpolated waypoints
            for (int j = 1; j <= waypointCount; j++){
                float t = (float)j / waypointCount;
                waypoints.Add(new PathPoint { position = 
                                                Vector3.Lerp(lastPoint, nextPoint, t)
                                                , targetHeading = heading
                                                , isCorner = false
                                            });
            }
            // Add end corner with heading to next corner
            if (i + 1 == corners.Length - 1) { // Checks if nextpoint is the last corner in the list
                waypoints.Add(new PathPoint {position = corners[i+1], targetHeading = 0.0f, isCorner = true});
            }
        }

    return waypoints;

    }

    // Calculate the desired omega relative to the world that the next corners targetHeading is pointing.
    // for waypoints we assign a changing desired omega for the car to match with 
    // these omega values will increase until matching the next corner 1 before the following corner
    private List<PathPoint> AppendDesiredOmega(List<PathPoint> waypoints) {
        for (int i = 0; i <waypoints.Count; i++){
            PathPoint current = waypoints[i];

            if (current.isCorner) {
                // Corners: desiredOmega = corner targetHeading
                waypoints[i] = new PathPoint {
                    position = current.position,
                    desiredOmega = current.targetHeading,
                    targetHeading = current.targetHeading,
                    isCorner = true
                };
            } else {
                // waypoints: each waypoint uses an algorithm to calculate its desired omega based on following logic
                // 1. Desired omega turns as late as possible based on CtrlFig's maxRotationSpeed rad/s
                // 2. DesiredOmega will point the vx (forward direction) towards the next corners targetHeading
                // 3. desiredOmega will be = the next corners targetHeading 1 waypoint before the next corner
                
                int nextCornerIndex = FindNextCorner(waypoints, i); // Find Next corner to determine targetHeading

                if (nextCornerIndex == -1){
                    Debug.Log("Failed to find next corner");
                    
                    waypoints[i] = new PathPoint { // Maintain target heading (just incase)
                    position = current.position,
                    desiredOmega = current.targetHeading,
                    targetHeading = current.targetHeading,
                    isCorner = false
                    };

                } else {
                    int waypointsUntilCorner = nextCornerIndex - i; // get number of waypoints until next corner
                    float currentHeading = current.targetHeading; // Get current waypoints target heading
                    float cornerHeading = waypoints[nextCornerIndex].targetHeading; // Get next corners target heading stored
                    float progressToCorner = (float)(waypointsUntilCorner - 1) / waypointsUntilCorner; // stores progress between 0 and 1 from current index to 1 before next corner

                    // Find angle between heading
                    float angleDiff = Mathf.DeltaAngle(
                        currentHeading * Mathf.Rad2Deg,
                        cornerHeading * Mathf.Rad2Deg
                    ) * Mathf.Deg2Rad;

                    float Time2Rotate = Mathf.Abs(angleDiff) / maxOmega;
                    float distanceNeeded = maxLinearVel * Time2Rotate;
                    float distToCorner = Vector3.Distance(current.position, waypoints[nextCornerIndex].position);
                    if (distToCorner < 0.0001f) distToCorner = 0.0001f;
                    float progress = 0.0f;

                    // 
                    if (distToCorner <= distanceNeeded)
                    {
                        progress = Mathf.Clamp01(
                            (distanceNeeded - distToCorner) / distanceNeeded
                        );
                    }

                    float desiredHeading = Mathf.LerpAngle(
                                                            currentHeading * Mathf.Rad2Deg,
                                                            cornerHeading * Mathf.Rad2Deg,
                                                            progress
                                                            ) * Mathf.Deg2Rad;
                    waypoints[i] = new PathPoint
                    {
                        position = current.position,
                        desiredOmega = desiredHeading,
                        targetHeading = current.targetHeading,
                        isCorner = false
                    };

                }

            }

        }
    return waypoints;


    }

    private int FindNextCorner(List<PathPoint> waypoints, int currentWaypoint){
        for (int i = currentWaypoint + 1; i < waypoints.Count; i++ ){
            if (waypoints[i].isCorner){
                return i;
            }
        }
        return -1;
    }

}