using UnityEngine;

// vx/vy arrive normalized [-1, 1].
// Physics velocity = normalized * MaxLinearSpeed (m/s).

[RequireComponent(typeof(Rigidbody))]
public class AIMovementController : MonoBehaviour
{
    private Rigidbody rb;
    public RobotConfig controlConfig;

    private float _vx, _vy, _omega;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float speed = controlConfig.MaxSpeed;

        // 🔹 Normalize input to prevent faster diagonal movement
        Vector3 input = new Vector3(_vx, 0f, _vy);
        input = Vector3.ClampMagnitude(input, 1f);

        // 🔹 Convert to world-space movement
        Vector3 worldMove =
            transform.right   * input.x +
            transform.forward * input.z;

        Vector3 worldVel = worldMove * speed;

        // 🔹 Apply velocity (preserve vertical velocity for gravity)
        rb.linearVelocity = new Vector3(
            worldVel.x,
            rb.linearVelocity.y,
            worldVel.z
        );

        // 🔹 Apply rotation (yaw only)
        rb.angularVelocity = new Vector3(0f, _omega, 0f);

        Debug.Log($"[AIMovementController] vx={_vx:F2}, vy={_vy:F2}, omega={_omega:F2} " +
                  $"=> worldVel=({worldVel.x:F2}, {worldVel.y:F2}, {worldVel.z:F2})");
    }

    public void ApplyVelocity(VelocityOutput velocity)
    {
        _vx    = velocity.vx;
        _vy    = velocity.vy;
        _omega = velocity.omega;
    }
}