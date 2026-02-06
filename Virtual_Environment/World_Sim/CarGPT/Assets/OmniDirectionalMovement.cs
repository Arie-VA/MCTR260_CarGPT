using UnityEngine;  // Needed for GameObject, Transform, Vector3, etc.

public class SimplePlayerMovement : MonoBehaviour
{
    public float speed = 2.0f;   // Movement speed
    public GameObject character; // Optional: reference to the cube, not strictly needed
    private Rigidbody rb; // Reference to the Rigidbody component
    private Quaternion originalRotation; // Store the original rotation

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
        originalRotation = transform.rotation; // Store the original rotation
    }

    void Update()
    {
        // Move right
        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.position += Vector3.right * speed * Time.deltaTime;
        }

        // Move left
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.position += Vector3.left * speed * Time.deltaTime;
        }

        // Move forward
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.position += Vector3.forward * speed * Time.deltaTime;
        }

        // Move back
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position += Vector3.back * speed * Time.deltaTime;
        }

        // Reset position to origin when 'R' is pressed
        if (Input.GetKey(KeyCode.R))
        {
            transform.position = new Vector3(0, 1, 0); // Reset to original position (assuming y=1 for the cube)
            rb.linearVelocity = Vector3.zero; // Reset velocity to stop any ongoing movement
            rb.angularVelocity = Vector3.zero; // Reset angular velocity to stop any ongoing rotation
            rb.rotation = Quaternion.identity; // Reset rotation to default
        }
    }
}
