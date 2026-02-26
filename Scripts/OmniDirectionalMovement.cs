using UnityEngine;

public class OmniDirectionalMovement : MonoBehaviour
{
    public HardwareConfig hardware;
    public float rotationSpeedFactor = 1f;

    private Vector2 currentLinearVelocity = Vector2.zero;
    private float currentAngularVelocity = 0f;

    // <<< These need to exist here as class members
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        // Store initial position and rotation
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // --- Read inputs ---
        float inputX = 0f;
        float inputY = 0f;
        float inputOmega = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) inputX += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) inputX -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) inputY += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) inputY -= 1f;
        if (Input.GetKey(KeyCode.A)) inputOmega += 1f;
        if (Input.GetKey(KeyCode.D)) inputOmega -= 1f;

        Vector2 inputVector = new Vector2(inputX, inputY);
        if (inputVector.magnitude > 1f) inputVector.Normalize();

        // --- Scale and limit velocities ---
        Vector2 desiredLinearVelocity = inputVector * hardware.MaxLinearSpeed;
        float desiredAngularVelocity = inputOmega * hardware.MaxAngularSpeed * rotationSpeedFactor;

        float deltaTime = Time.deltaTime;

        // Linear acceleration limit
        Vector2 linearDelta = desiredLinearVelocity - currentLinearVelocity;
        float maxLinearDelta = hardware.MaxLinearAcceleration * deltaTime;
        if (linearDelta.magnitude > maxLinearDelta)
            linearDelta = linearDelta.normalized * maxLinearDelta;
        currentLinearVelocity += linearDelta;

        // Angular acceleration limit
        float angularDelta = desiredAngularVelocity - currentAngularVelocity;
        float maxAngularDelta = hardware.MaxAngularAcceleration * deltaTime;
        angularDelta = Mathf.Clamp(angularDelta, -maxAngularDelta, maxAngularDelta);
        currentAngularVelocity += angularDelta;

        // --- Move cube ---
        transform.Translate(new Vector3(currentLinearVelocity.x, 0f, currentLinearVelocity.y) * deltaTime, Space.World);
        transform.Rotate(Vector3.up, currentAngularVelocity * Mathf.Rad2Deg * deltaTime);

        // --- Reset ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Reset position and rotation
            transform.position = startPosition;
            transform.rotation = startRotation;

            // Reset speed
            currentLinearVelocity = Vector2.zero;
            currentAngularVelocity = 0f;

            Debug.Log("Cube reset to start position and speed.");
        }
    }
}