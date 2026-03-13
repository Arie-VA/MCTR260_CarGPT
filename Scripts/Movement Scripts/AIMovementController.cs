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
        Debug.Log($"usableMaxSpeed on awake: {controlConfig.usableMaxSpeed}");
    }

    void FixedUpdate()
    {
        float usableSpeed = (float)controlConfig.usableMaxSpeed;
        rb.WakeUp();
        // Compute local-frame velocity in world coordinates
        Vector3 worldVel = transform.right * _vx * usableSpeed
                       + transform.forward * _vy * usableSpeed;

        Debug.Log($"_vx: {_vx} _vy: {_vy} worldvel.x {worldVel.x}, worldvel.z {worldVel.z} useSpeed: {usableSpeed}");
    
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