using UnityEngine;

public enum ObjectType { Box, Ball }
public enum ZoneNum { One = 1, Two = 2, Three = 3 }
public enum ZoneType { Pickup, Dropoff }

[RequireComponent(typeof(Collider))]
public class DetectedObject : MonoBehaviour
{
    [Header("Object Properties")]
    public ObjectType type;         // Box or Ball
    public int zoneDesignation;     // 1, 2, or 3
    public int basePointValue = 10;

    [Header("Current Status")]
    public ZoneType? currentZoneType = null;
    public ZoneNum? currentZoneNum = null;
    public int currentPointValue;
    public bool isInAllowedZone;

    [Header("Zone Settings")]
    public Transform pickupZone;
    public Transform dropoffZone;
    public Vector2 ZoneSize = new Vector2(1f, 1f);

    private void Awake()
    {
        currentPointValue = basePointValue;
        UpdateZoneStatus();
    }

    private void Update()
    {
        UpdateZoneStatus();
    }

    public void UpdateZoneStatus()
    {
        Vector2 posXZ = new Vector2(transform.position.x, transform.position.z);

        isInAllowedZone = false;
        currentZoneType = null;
        currentZoneNum = null;

        // Check pickup zone
        if (pickupZone != null)
        {
            Vector2 pickupXZ = new Vector2(pickupZone.position.x, pickupZone.position.z);
            if (IsInsideRect(posXZ, pickupXZ, ZoneSize))
            {
                currentZoneType = ZoneType.Pickup;   // Fixed: was currentZone
                currentZoneNum = (ZoneNum)zoneDesignation;
                isInAllowedZone = true;
            }
        }

        // Check dropoff zone
        if (!isInAllowedZone && dropoffZone != null)
        {
            Vector2 dropXZ = new Vector2(dropoffZone.position.x, dropoffZone.position.z);
            if (IsInsideRect(posXZ, dropXZ, ZoneSize))
            {
                currentZoneType = ZoneType.Dropoff;  // Fixed: was currentZone
                currentZoneNum = (ZoneNum)zoneDesignation;
                isInAllowedZone = true;
            }
        }

        // Misplaced objects get higher priority
        currentPointValue = isInAllowedZone ? basePointValue : basePointValue * 5;
    }

    private bool IsInsideRect(Vector2 posXZ, Vector2 centerXZ, Vector2 size)
    {
        float halfWidth  = size.x / 2f;
        float halfLength = size.y / 2f;

        return posXZ.x >= centerXZ.x - halfWidth &&
               posXZ.x <= centerXZ.x + halfWidth &&
               posXZ.y >= centerXZ.y - halfLength &&
               posXZ.y <= centerXZ.y + halfLength;
    }
}