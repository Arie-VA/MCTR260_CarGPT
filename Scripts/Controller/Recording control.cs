using UnityEngine;

/// <summary>
/// Simple recording control via keyboard.
/// Attach to any GameObject, assign ManualRecorder field.
/// </summary>
public class RecordingControl : MonoBehaviour
{
    [SerializeField] private ManualRecorder manualRecorder;

    void Update()
    {
        if (manualRecorder == null) return;

        // Press '1' for Pickup Zone 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            manualRecorder.StartRecording("Pickup_Zone1");
            Debug.Log("▶ Recording: Pickup_Zone1 (drive to zone 1 and pick up)");
        }

        // Press '2' for Pickup Zone 2
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            manualRecorder.StartRecording("Pickup_Zone2");
            Debug.Log("▶ Recording: Pickup_Zone2 (drive to zone 2 and pick up)");
        }

        // Press '3' for Dropoff Zone 1
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            manualRecorder.StartRecording("Dropoff_Zone1");
            Debug.Log("▶ Recording: Dropoff_Zone1 (drive to zone 1 and drop)");
        }

        // Press '4' for Dropoff Zone 2
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            manualRecorder.StartRecording("Dropoff_Zone2");
            Debug.Log("▶ Recording: Dropoff_Zone2 (drive to zone 2 and drop)");
        }

        // STOP CURRENT RECORDING
        if (Input.GetKeyDown(KeyCode.E))
        {
            bool success = manualRecorder.EndRecording();
            if (success) 
            {
                Debug.Log("✓ Recording stopped and saved!");
            }
        }
    }
}