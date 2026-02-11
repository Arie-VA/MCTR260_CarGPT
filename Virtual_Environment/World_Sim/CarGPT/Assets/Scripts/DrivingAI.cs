using UnityEngine;

public class DrivingModule : MonoBehaviour
{
    public Transform target; // The AI goal
    public float maxSpeed = 1f;      // meters per second
    public float maxAngular = 90f;   // degrees per second
    public float L = 0.5f, W = 0.4f, R = 0.05f; // robot dimensions

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    
    void Update()
    {
        if (target == null) return;

        //  Direction vector
        Vector3 dir = target.position - transform.position;
        Vector3 dirXZ = new Vector3(dir.x, 0, dir.z);
        float distance = dirXZ.magnitude;
        if (distance < 0.1f) return;

        dirXZ.Normalize();

        //  Convert to local velocities
        Vector3 localDir = transform.InverseTransformDirection(dirXZ);
        float vx = localDir.z * maxSpeed; // forward
        float vy = localDir.x * maxSpeed; // sideways
        float desiredAngle = Mathf.Atan2(dirXZ.x, dirXZ.z) * Mathf.Rad2Deg;
        float angleDiff = Mathf.DeltaAngle(transform.eulerAngles.y, desiredAngle);
        float omega = Mathf.Clamp(angleDiff, -maxAngular * Time.deltaTime, maxAngular * Time.deltaTime);

        //  Mecanum wheel speeds
        float w1 = (vx - vy - (L + W) * omega * Mathf.Deg2Rad) / R;
        float w2 = (vx + vy + (L + W) * omega * Mathf.Deg2Rad) / R;
        float w3 = (vx + vy - (L + W) * omega * Mathf.Deg2Rad) / R;
        float w4 = (vx - vy + (L + W) * omega * Mathf.Deg2Rad) / R;

        Debug.Log($"Wheel speeds: {w1:F2}, {w2:F2}, {w3:F2}, {w4:F2}");

        // Move the box in Unity
        transform.Rotate(0, omega, 0);
        transform.position += transform.forward * vx * Time.deltaTime + transform.right * vy * Time.deltaTime;
    }
}
