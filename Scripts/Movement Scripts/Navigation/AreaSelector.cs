using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaSelector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float horizontalGroupingThreshold = 0.5f; // Objects closer than this X distance are grouped

    [Header("Internal")]
    public List<DetectedObject> allObjects = new List<DetectedObject>();

    /// Returns the best target area based on point value and horizontal grouping
    public Vector3? GetBestTargetArea()
    {
        if (allObjects == null || allObjects.Count == 0) return null;

        // Step 1: Filter for objects eligible for pickup
        var pickupCandidates = allObjects.Where(o =>
            !o.isInAllowedZone || (o.currentZoneType == ZoneType.Pickup)
        ).ToList();

        if (pickupCandidates.Count == 0) return null;

        // Step 2: Sort by X coordinate for horizontal grouping
        pickupCandidates = pickupCandidates.OrderBy(o => o.transform.position.x).ToList();

        // Step 3: Group objects by horizontal (XZ) proximity
        List<List<DetectedObject>> groups = new List<List<DetectedObject>>();
        List<DetectedObject> currentGroup = new List<DetectedObject>();

        currentGroup.Add(pickupCandidates[0]);

        for (int i = 1; i < pickupCandidates.Count; i++)
        {
            var prev = pickupCandidates[i - 1];
            var curr = pickupCandidates[i];

            // Use XZ-plane distance (ignore Y)
            Vector2 prevXZ = new Vector2(prev.transform.position.x, prev.transform.position.z);
            Vector2 currXZ = new Vector2(curr.transform.position.x, curr.transform.position.z);

            if (Vector2.Distance(currXZ, prevXZ) <= horizontalGroupingThreshold)
            {
                currentGroup.Add(curr);
            }
            else
            {
                groups.Add(new List<DetectedObject>(currentGroup));
                currentGroup.Clear();
                currentGroup.Add(curr);
            }
        }

        if (currentGroup.Count > 0)
    groups.Add(currentGroup);

        // Step 4: Score each group based on total points
        float bestScore = float.MinValue;
        List<DetectedObject> bestGroup = null;

        foreach (var group in groups)
        {
            float groupScore = group.Sum(o => o.currentPointValue);
            if (groupScore > bestScore)
            {
                bestScore = groupScore;
                bestGroup = group;
            }
        }

        // Step 5: Return the centroid of the best group as target
        if (bestGroup != null && bestGroup.Count > 0)
        {
            Vector3 centroid = Vector3.zero;
            foreach (var obj in bestGroup)
            {
                centroid += obj.transform.position;
            }
            centroid /= bestGroup.Count;
            return centroid;
        }

        return null;
    }
}