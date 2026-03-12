using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaSelector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float horizontalGroupingThreshold = 0.5f; // Objects closer than this XZ distance are grouped

    [Header("Internal")]
    public List<DetectedObject> allObjects = new List<DetectedObject>();

    /// Returns the centroid and zone designation of the highest scoring pickup group.
    /// Returns null if no valid targets exist.
    public (Vector3 centroid, int zoneDesignation)? GetBestTargetArea()
    {
        if (allObjects == null || allObjects.Count == 0) return null;

        // Step 1: Filter for objects eligible for pickup
        var pickupCandidates = allObjects.Where(o =>
            !o.isInAllowedZone || o.currentZoneType == ZoneType.Pickup
        ).ToList();

        if (pickupCandidates.Count == 0) return null;

        // Step 2: Sort by X coordinate for horizontal grouping
        pickupCandidates = pickupCandidates.OrderBy(o => o.transform.position.x).ToList();

        // Step 3: Group objects by horizontal (XZ) proximity
        List<List<DetectedObject>> groups = new List<List<DetectedObject>>();
        List<DetectedObject> currentGroup = new List<DetectedObject> { pickupCandidates[0] };

        for (int i = 1; i < pickupCandidates.Count; i++)
        {
            var prev = pickupCandidates[i - 1];
            var curr = pickupCandidates[i];

            Vector2 prevXZ = new Vector2(prev.transform.position.x, prev.transform.position.z);
            Vector2 currXZ = new Vector2(curr.transform.position.x, curr.transform.position.z);

            if (Vector2.Distance(currXZ, prevXZ) <= horizontalGroupingThreshold)
                currentGroup.Add(curr);
            else
            {
                groups.Add(new List<DetectedObject>(currentGroup));
                currentGroup.Clear();
                currentGroup.Add(curr);
            }
        }

        if (currentGroup.Count > 0)
            groups.Add(currentGroup);

        // Step 4: Score each group by total point value
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

        if (bestGroup == null || bestGroup.Count == 0) return null;

        // Step 5: Calculate centroid
        Vector3 centroid = Vector3.zero;
        foreach (var obj in bestGroup)
            centroid += obj.transform.position;
        centroid /= bestGroup.Count;

        // Step 6: Determine zone designation by majority vote within the group
        int zoneDesignation = bestGroup
            .GroupBy(o => o.zoneDesignation)
            .OrderByDescending(g => g.Count())
            .First().Key;

        return (centroid, zoneDesignation);
    }
}