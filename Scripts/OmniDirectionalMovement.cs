using UnityEngine;

public class OmniDirectionalMovement : MonoBehaviour
{
    public ControlConfig controlConfig;
    public float rotationSpeedFactor = 1f;

    private Vector2 currentLinearVelocity = Vector2.zero;
    private float currentAngularVelocity = 0f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (controlConfig == null)
            Debug.LogError("OmniDirectionalMovement: ControlConfig not assigned!");
    }

    void Update()
    {
        if (controlConfig == null) return;

        // --- Read inputs ---
        float inputX     = 0f;
        float inputY     = 0f;
        float inputOmega = 0f;

        if (Input.GetKey(KeyCode.UpArrow))    inputX     += 1f;
        if (Input.GetKey(KeyCode.DownArrow))  inputX     -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) inputY     += 1f;
        if (Input.GetKey(KeyCode.LeftArrow))  inputY     -= 1f;
        if (Input.GetKey(KeyCode.A))          inputOmega += 1f;
        if (Input.GetKey(KeyCode.D))          inputOmega -= 1f;

        Vector2 inputVector = new Vector2(inputX, inputY);
        if (inputVector.magnitude > 1f) inputVector.Normalize();

        // --- Scale to hardware limits ---
        Vector2 desiredLinearVelocity  = inputVector * controlConfig.MaxLinearSpeed;
        float desiredAngularVelocity   = inputOmega  * controlConfig.MaxAngularSpeed * rotationSpeedFactor;

        float dt = Time.deltaTime;

        // --- Linear acceleration limiting ---
        Vector2 linearDelta    = desiredLinearVelocity - currentLinearVelocity;
        float maxLinearDelta   = controlConfig.MaxLinearAcceleration * dt;
        if (linearDelta.magnitude > maxLinearDelta)
            linearDelta = linearDelta.normalized * maxLinearDelta;
        currentLinearVelocity += linearDelta;

        // --- Angular acceleration limiting ---
        float angularDelta   = desiredAngularVelocity - currentAngularVelocity;
        float maxAngularDelta = controlConfig.MaxAngularAcceleration * dt;
        angularDelta          = Mathf.Clamp(angularDelta, -maxAngularDelta, maxAngularDelta);
        currentAngularVelocity += angularDelta;

        // --- Apply motion ---
        transform.Translate(new Vector3(currentLinearVelocity.x, 0f, currentLinearVelocity.y) * dt, Space.World);
        transform.Rotate(Vector3.up, currentAngularVelocity * Mathf.Rad2Deg * dt);

        // --- Reset ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position     = startPosition;
            transform.rotation     = startRotation;
            currentLinearVelocity  = Vector2.zero;
            currentAngularVelocity = 0f;
            Debug.Log("Robot reset to start position.");
        }
    }
}