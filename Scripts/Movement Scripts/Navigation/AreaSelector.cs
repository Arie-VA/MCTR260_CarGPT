using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaSelector : MonoBehaviour
{
   
    [Header("Internal")]
    public List<DetectedObject> allObjects = new List<DetectedObject>();

    // Zones that have already been visited — excluded from all future scoring
    private HashSet<int> _visitedZones = new HashSet<int>();

    public void MarkZoneVisited(int zoneDesignation)
    {
        _visitedZones.Add(zoneDesignation);
        Debug.Log($"[AreaSelector] Zone {zoneDesignation} marked as visited. " +
                  $"Blacklisted zones: [{string.Join(", ", _visitedZones)}]");
    }

    public void ResetVisitedZones()
    {
        _visitedZones.Clear();
        Debug.Log("[AreaSelector] Visited zone blacklist cleared.");
    }

    /// Returns the zone designation of the highest scoring pickup group.
    /// Returns null if no valid targets exist.
 public int? GetBestTargetArea()
    {
        if (allObjects == null || allObjects.Count == 0) return null;

        // Debug all objects
        foreach (var o in allObjects)
        {
            Debug.Log($"[AreaSelector] obj={o.name} zone={o.zoneDesignation} " +
                    $"inZone={o.isInAllowedZone} zoneType={o.currentZoneType} " +
                    $"points={o.currentPointValue} visited={_visitedZones.Contains(o.zoneDesignation)}");
        }

        var pickupCandidates = allObjects.Where(o =>
            (!o.isInAllowedZone || o.currentZoneType == ZoneType.Pickup) &&
            !_visitedZones.Contains(o.zoneDesignation)
        ).ToList();

        if (pickupCandidates.Count == 0) return null;

        var bestZone = pickupCandidates
            .GroupBy(o => o.zoneDesignation)
            .Select(g => new { Zone = g.Key, Score = g.Sum(o => o.currentPointValue) })
            .OrderByDescending(g => g.Score)
            .FirstOrDefault();

        return bestZone?.Zone;
    }
}