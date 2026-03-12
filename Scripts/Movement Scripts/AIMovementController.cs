using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIMovementController : MonoBehaviour
{
    private Rigidbody rb;
    public ControlConfig controlConfig;
    private float _vx, _vy, _omega;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float usableMaxSpeed = controlConfig.usableMaxSpeed;
        rb.WakeUp();
        // Compute local-frame velocity in world coordinates
        Vector3 worldVel = transform.right * _vx * usableMaxSpeed
                       + transform.forward * _vy * usableMaxSpeed;

        // Preserve Y (gravity) velocity
        rb.linearVelocity = new Vector3(worldVel.x, rb.linearVelocity.y, worldVel.z);

        // Apply rotation
        rb.angularVelocity = new Vector3(0f, _omega * 2, 0f);
        //rb.angularVelocity = new Vector3(0f, 3f, 0f);  // 3 rad/s

    }

    public void ApplyVelocity(VelocityOutput velocity)
    {
        // vx = forward, vy = sideways relative to robot
        _vx = velocity.vx;
        _vy = velocity.vy;
        _omega = velocity.omega;
    }
}