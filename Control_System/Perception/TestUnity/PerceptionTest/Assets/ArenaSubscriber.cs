using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

[System.Serializable]
public class DetectionData
{
    // This class matches the data published by Perception.py to the /arena/detections message.
    public string label;
    public float x;
    public float y;

}

public class ArenaSubscriber : MonoBehaviour
{
    [Header("Tracking Setup")]
    public GameObject trackedObjectPrefab;
    private GameObject currentTrackedObject;

    void Start()
    {
        // Hook into the ROS Connection and subscribe to the topic
        ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>("/arena/detections", ProcessDetection);
        Debug.Log("Subscribed to /arena/detections");
    }
    
    // This will run every time a new message arrives from WSL
    void ProcessDetection(StringMsg msg)
    {
        // Getting the json from the perception.py message
        string jsonString = msg.data;
        Debug.Log("Recieved from ROS: " + jsonString);
        // Converting the extracted string to a DetectionData object
        DetectionData data = JsonUtility.FromJson<DetectionData>(jsonString);

        if (data == null)
        {
            Debug.LogError("Failed to parse the JSON string");
            return;
        }

        if (trackedObjectPrefab == null)
        {
            Debug.LogError("Prefab in ROS_Origin is not configured right");
            return;
        }


        // Instantiate the object if it doesn't exist
        if (currentTrackedObject == null)
        {
            currentTrackedObject = Instantiate(trackedObjectPrefab);
            currentTrackedObject.transform.SetParent(this.transform, false);
        }

        // Map the coordinates. Python sends 2d (x, y) while Unity uses 3d (x,y,z). The real x is the Unity X, the real y is the unity z.
        // We set the y coordinate to be just a bit above the floor.
        Vector3 targetPosition = new Vector3(data.x,0.05f,data.y);
        currentTrackedObject.transform.localPosition = targetPosition;
        // Unnecessary: Update the name
        currentTrackedObject.name = "Detected: " + data.label;
    }
}
