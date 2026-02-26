using UnityEngine;

public enum ObjectType { Box, Ball }
public enum ZoneNum { One = 1, Two = 2, Three = 3}
public enum ZoneType { Pickup, Dropoff}

[RequireComponent(typeof(Collider))]
public class DetectedObject : MonoBehaviour
{
    [Header("Object Properties")]
    public ObjectType type;         // Box or Ball
    public int zoneDesignation;     // 1, 2, or 3
    public int basePointValue = 10; // base points for this type

    [Header("Current Status")]
    public ZoneType? currentZoneType = null;
    public ZoneNum? currentZoneNum = null;   // Which zone (null if outside)
    public int currentPointValue;            // Updated dynamically
    public bool isInAllowedZone;             // True if object is in pickup or dropoff of its designation



    [Header("Zone Settings")]
    public Transform pickupZone;  // Transform of the assigned pickup zone
    public Transform dropoffZone; // Transform of the assigned dropoff zone
    public Vector2 ZoneSize = new Vector2(1f, 1f); // size of the zone for detection

    private void Awake()
    {
        // Initialize point value based on type
        currentPointValue = basePointValue;
        UpdateZoneStatus();
    }

    private void Update()
    {
        // Each frame, update which zone it is in and the point value
        UpdateZoneStatus();
    }


    /// Updates the object's current zone, in-correct-zone status, and point value
    public void UpdateZoneStatus()
    {
        Vector2 posXZ = new Vector2(transform.position.x, transform.position.z);

        isInAllowedZone = false; // reset status each update
        currentZoneType = null;
        currentZoneNum = null;


        // Check pickup zone
        if (pickupZone != null)
        {
            Vector2 pickupXZ = new Vector2(pickupZone.position.x, pickupZone.position.z);
            if (IsInsideRect(posXZ, pickupXZ, ZoneSize))
            {
                currentZone = ZoneType.Pickup;
                currentZoneNum = zoneDesignation;
                isInAllowedZone = true;
            }
        }

        // Check dropoff zone
        if (!isInAllowedZone && dropoffZone != null)
        {
            Vector2 dropXZ = new Vector2(dropoffZone.position.x, dropoffZone.position.z);
            if (IsInsideRect(posXZ, dropXZ, ZoneSize))
            {
                currentZone = ZoneType.Dropoff;
                currentZoneNum = zoneDesignation;
                isInAllowedZone = true;
            }
        }

        // If outside all assigned zones, give it high priority (increase points)
        if (!isInAllowedZone)
        {
            currentPointValue = basePointValue * 5; // arbitrary high weight for misplaced objects
        }
        else
        {
            currentPointValue = basePointValue; // normal points
        }
    }

    // Returns true if posXZ is inside rectangle centered at centerXZ with size (width, length)
    private bool IsInsideRect(Vector2 posXZ, Vector2 centerXZ, Vector2 size)
    {
        float halfWidth = size.x / 2f;
        float halfLength = size.y / 2f;

        return posXZ.x >= centerXZ.x - halfWidth &&
               posXZ.x <= centerXZ.x + halfWidth &&
               posXZ.y >= centerXZ.y - halfLength &&
               posXZ.y <= centerXZ.y + halfLength;
    }
}