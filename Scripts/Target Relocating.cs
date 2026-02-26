using UnityEngine;
using System;

public class TargetRelocatingSingle : MonoBehaviour
{
    public Transform box;          // The AI box
    public float threshold = 0.3f; // How close before considered reached

    [System.Serializable]
    public class Job
    {
        public Transform pickup;
        public Transform dropoff;
    }

    public Job jobA;
    public Job jobB;
    public Job jobC;

    private Job currentJob;
    private bool headingToPickup = true; // Are we heading to pickup or dropoff?
    private DrivingModule driver;

    private Job[] allJobs;

    void Start()
    {
        driver = box.GetComponent<DrivingModule>();

        // Put all jobs into an array for easy random selection
        allJobs = new Job[] { jobA, jobB, jobC };

        // Pick a random job to start
        currentJob = allJobs[UnityEngine.Random.Range(0, allJobs.Length)];
        headingToPickup = true;

        // Set the first target
        driver.SetTarget(currentJob.pickup);
    }

    void Update()
    {
        if (box == null || currentJob == null) return;

        Transform currentGoal = headingToPickup ? currentJob.pickup : currentJob.dropoff;
        float distance = Vector3.Distance(box.position, currentGoal.position);

        if (distance < threshold)
        {
            if (headingToPickup)
            {
                // Reached pickup → head to dropoff
                headingToPickup = false;
                driver.SetTarget(currentJob.dropoff);
                Debug.Log($"Picked up box, now heading to dropoff: {currentJob.dropoff.name}");
            }
            else
            {
                // Reached dropoff → pick a new random pickup
                currentJob = allJobs[UnityEngine.Random.Range(0, allJobs.Length)];
                headingToPickup = true;
                driver.SetTarget(currentJob.pickup);
                Debug.Log($"Dropped off box, now heading to new pickup: {currentJob.pickup.name}");
            }
        }
    }
}
