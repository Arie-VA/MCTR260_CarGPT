using UnityEngine;
using System.Collections.Generic;

public class WarehouseAIController : MonoBehaviour
{
    [Header("References")]
    public AreaSelector    areaSelector;
    public PathPlanner     pathPlanner;
    public RobotConfig     config;
    public PathController  pathController;
    public Transform       pickupZone1;
    public Transform       pickupZone2;
    public Transform       pickupZone3;
    public Transform       dropoffZone1;
    public Transform       dropoffZone2;
    public Transform       dropoffZone3;

    [Header("Perception")]
    public Perception perception;
    public Transform carGPT;

    [Header("UDP")]
    public RobotUDPSender udpSender;

    [Header("Manual Override")]
    public ManualController manualController;

    [Header("Recording/Playback")]
    public ManualRecorder manualRecorder;



    // --- State ---
    private bool hasObjects = false;
    private bool hasTarget  = false;
    private bool atTarget   = false;

    // --- Path data ---
    private Vector3[]                    currentPath = new Vector3[0];
    private int                          currentTargetZone;
    private PathFollower                 pathFollower;
    private List<PathFollower.PathPoint> currentWaypoints;
    private bool                         pathLoaded = false;

    // --- Stepper state ---
    private bool  _stepperActive      = false;
    private float _stepperCompleteTime = 0f;

    // --- Heading smoothing (decoupled from perception jitter) ---
    private float _smoothedHeading = 0f;
    private float _targetHeading = 0f;
    private float _headingSmoothVelocity = 0f;  // For SmoothDamp
    private bool  _headingInitialized = false;
    private const float HeadingSmoothTime = 0.08f;  // Damped smoothing (80ms settle)

    // --- Position smoothing (decoupled from perception teleport) ---
    private Vector3 _smoothedPosition = Vector3.zero;
    private Vector3 _targetPosition = Vector3.zero;
    private Vector3 _positionSmoothVelocity = Vector3.zero;  // For SmoothDamp
    private bool    _positionInitialized = false;
    private const float PositionSmoothTime = 0.1f;  // Damped smoothing (100ms settle)
    private bool  _isUsingRecording   = false;
    private float _lastPerceptionTime = 0f;
    private const float PERCEPTION_TIMEOUT = 0.6f;   // 600 ms — generous but safe

    // --- 20 Hz throttling ---
    private const float UpdateInterval = 1f / 20f;  // 0.05 seconds
    private float timeSinceLastUpdate = 0f;

    // ─────────────────────────────────────────────
   void Awake()
    {
        if (config == null)
        {
            Debug.LogError("WarehouseAIController: RobotConfig not assigned!");
            return;
        }

        if (udpSender == null)
        {
            Debug.LogError("WarehouseAIController: RobotUDPSender not assigned!");
            return;
        }

        pathFollower = new PathFollower(config);
        pathFollower.Initialize();

        // FIX: Force the initial state variables directly instead of calling Dropoff()
        hasObjects           = false;
        hasTarget            = false;
        atTarget             = false;
        _stepperActive       = false;
        _stepperCompleteTime = 0f;

        if (manualController != null)
        {
            manualController.IsOverrideActive = true;
            Debug.Log("[AI] Starting in MANUAL mode");
        }
        Debug.LogError($"HasTarget {hasTarget}; pathLoaded = {pathLoaded}, hasObjects {hasObjects}");
    }

    // ─────────────────────────────────────────────
    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate < UpdateInterval)
            return;

        timeSinceLastUpdate -= UpdateInterval;

        UpdateFromPerception();
        ApplySmoothingInControlLoop();


        // 1. MANUAL OVERRIDE (High priority)
        if (manualController != null && manualController.IsOverrideActive)
        {
            hasObjects = false; // Force AI to ignore current state and just do what manual commands say
            return;
        }
            

        // 2. PLAYBACK DRIVE (The missing link)
        if (manualRecorder != null && manualRecorder.IsPlaying)
        {
            // Fetch the velocity from the recording
            VelocityOutput playbackV = manualRecorder.GetPlaybackVelocity();
            
            // SEND it to the robot
            udpSender?.Send(playbackV, $"Playback: {manualRecorder.name}");
            
            // Still return so the normal AI state machine doesn't interfere
            return; 
        }

        // --- DEBUG: Recording/Playback Keyboard Controls ---
        UpdateDebugPlayback();
        Debug.LogWarning($"[AI] State | hasObjects: {hasObjects} | hasTarget: {hasTarget} | atTarget: {atTarget} | stepperActive: {_stepperActive} | usingRecording: {_isUsingRecording}");
        // 3. NORMAL AI STATE MACHINE
        if      (!hasTarget && !atTarget)      Pathing();
        else if ( hasTarget && !atTarget)      Moving2Target();
        else if (!hasObjects && atTarget)      Pickup();
        else if ( hasObjects && atTarget)      Dropoff();
    }

    // ─────────────────────────────────────────────
    private void Pathing()
    {
        Vector3 destination;

        if (!hasObjects)
        {
            int? result = areaSelector.GetBestTargetArea();
            if (result == null)
            {
                Debug.Log("Pathing: No valid pickup targets found. Staying idle.");
                return;
            }

            currentTargetZone = result.Value;

            Transform pickupTransform = currentTargetZone switch
            {
                1 => pickupZone1,
                2 => pickupZone2,
                3 => pickupZone3,
                _ => pickupZone1 // Default to zone 1 if something goes wrong
            };

            if (pickupTransform == null)
            {
                Debug.LogError($"Pathing: No pickup zone transform for zone {currentTargetZone}.");
                return;
            }

            destination = pickupTransform.position;
            Debug.Log($"[WarehouseAI] Navigating to pickup zone {currentTargetZone} at {destination}");
        }
        else
        {
            Transform dropoff = currentTargetZone switch
            {
                1 => dropoffZone1,
                2 => dropoffZone2,
                3 => dropoffZone3,
                _ => pickupZone1 // Default to zone 1 if something goes wrong
            };

            if (dropoff == null)
            {
                Debug.LogError($"Pathing: No dropoff zone for zone {currentTargetZone}.");
                return;
            }

            destination = dropoff.position;
        }

        Vector3[] path = pathPlanner.GeneratePath(transform.position, destination);

        if (path.Length == 0)
        {
            Debug.LogWarning($"Pathing: Empty path to {destination}.");
            return;
        }

        currentPath = path;
        hasTarget   = true;
        pathLoaded  = false;
 

        Debug.LogError($"HasTarget {hasTarget}; pathLoaded = {pathLoaded}, hasObjects {hasObjects}");
        Debug.LogWarning("Pathing complete - should happen once per trip");
    }

    // ─────────────────────────────────────────────
    private void Moving2Target()
    {
        if (pathFollower == null || currentPath.Length == 0)
        {
            Debug.LogWarning("Moving2Target: No path available.");
            return;
        }

        if (!pathLoaded)
        {
            currentWaypoints = pathFollower.SetPath(currentPath);
            pathController.SetWaypoints(currentWaypoints);
            pathLoaded = true;
            Debug.LogWarning("Waypoints loaded into controller - should happen once per trip");
        }

        if (pathController.HasReachedDestination())
        {
            Debug.LogWarning("Destination reached.");
            atTarget   = true;
            hasTarget  = false;
            pathLoaded = false;
            Debug.LogError($"HasTarget {hasTarget}; pathLoaded = {pathLoaded}, hasObjects {hasObjects}");

            _stepperActive = false;
            _isUsingRecording = false;
            _stepperCompleteTime = 0f;

            VelocityOutput stop = new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 };
            udpSender?.Send(stop, "Moving2Target, stop sent, has reached destination");
            return;
        }

        VelocityOutput velocity = pathController.ComputeVelocity(
            _smoothedPosition,
            QuaternionFromHeading(_smoothedHeading)
        );
        udpSender?.Send(velocity, "Velocity Output in Moving2Target");
    }


    // ─────────────────────────────────────────────
    private void Pickup()
    {
        Debug.LogWarning("Starting Pickup sequence...");
        if (currentTargetZone == 0) 
        {
            currentTargetZone = 1; // Default to zone 1 if something goes wrong
        }
        // 1. STARTING PHASE
        if (!_stepperActive)
        {
            string label = $"Pickup_Zone{currentTargetZone}";
            
            if (manualRecorder != null && manualRecorder.StartPlayback(label))
            {
                _stepperActive = true;
                _isUsingRecording = true; // Mark that we are in recording mode
                return; 
            }
            else
            {
                // FALLBACK: Only if no recording exists
                _stepperCompleteTime = Time.time + config.PickupDuration;
                _stepperActive = true;
                _isUsingRecording = false;
            }
        }

        // 2. MONITORING PHASE
        if (_isUsingRecording)
        {
            // If the recorder finished the clip, complete the state
            if (!manualRecorder.IsPlaying)
            {
                CompletePickupState();
            }
            // While playing, we send NOTHING from here because Update() 
            // handles the playback commands.
            return; 
        }

        // 3. FALLBACK TIMER (Only runs if _isUsingRecording is false)
        VelocityOutput hold = new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 100f };
        udpSender?.Send(hold, "Hold Stepper in pickup fallback");

        if (Time.time > _stepperCompleteTime) 
        {
            CompletePickupState();
        }
    }

    private void CompletePickupState()
    {
        Debug.Log("[Pickup] Complete.");
        hasObjects = true;   // ← We picked something up
        hasTarget = false;   // ← Reset: no path to dropoff yet
        atTarget = false;    // ← Reset: we're not at dropoff yet (moving TO dropoff next)
        _stepperActive = false;
        _stepperCompleteTime = 0f;
        
        areaSelector.MarkZoneVisited(currentTargetZone);
        Debug.LogError($"HasTarget {hasTarget}; pathLoaded = {pathLoaded}, hasObjects {hasObjects}");
    }
    // ─────────────────────────────────────────────
    private void Dropoff()
    {
        // 0. GUARD: Prevent dropoff logic if no zone is actually targeted (e.g., at startup)
        if (currentTargetZone == 0) 
        {
            return;
        }

        // 1. STARTING PHASE: Determine if we use a recording or the timer fallback
        if (!_stepperActive)
        {
            string label = $"Dropoff_Zone{currentTargetZone}";
            
            // Try to play the recording first
            if (manualRecorder != null && manualRecorder.StartPlayback(label))
            {
                _stepperActive = true;
                _isUsingRecording = true; // State gate: We are in playback mode
                Debug.Log($"[Dropoff] 🎬 Starting playback for {label}");
                return;
            }
            else
            {
                // FALLBACK: Only if no recording is found for this zone
                Debug.LogWarning($"[Dropoff] No recording found for {label}. Using timer.");
                _stepperCompleteTime = Time.time + config.DropoffDuration;
                _stepperActive = true;
                _isUsingRecording = false; // State gate: We are in manual timer mode
            }
        }

        // 2. MONITORING PHASE: Playback Mode
        if (_isUsingRecording)
        {
            // If the recorder is no longer playing, the sequence is done
            if (manualRecorder == null || !manualRecorder.IsPlaying)
            {
                CompleteDropoffState();
            }
            // IMPORTANT: We return here so the fallback timer code below 
            // never sends "stepper = -100f" while the recording is active.
            return; 
        }

        // 3. MONITORING PHASE: Fallback Timer Mode
        // This only runs if _isUsingRecording is false.
        VelocityOutput hold = new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = -100f };
        udpSender?.Send(hold, "Dropoff hold stepper (Fallback)");

        if (Time.time > _stepperCompleteTime)
        {
            VelocityOutput stop = new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 };
            udpSender?.Send(stop, "Dropoff stop stepper (Fallback)");
            CompleteDropoffState();
        }
    }

   private void CompleteDropoffState()
    {
        Debug.Log("[Dropoff] Complete. Resetting state.");
        
        // Mark this zone as visited so AreaSelector won't pick it again
        if (areaSelector != null && currentTargetZone > 0)
        {
            areaSelector.MarkZoneVisited(currentTargetZone);
        }
        
        hasObjects           = false;
        hasTarget            = false;
        atTarget             = false;
        currentTargetZone    = 0;
        _stepperActive       = false;
        _stepperCompleteTime = 0f;
        
        Debug.LogError($"HasTarget {hasTarget}; pathLoaded = {pathLoaded}, hasObjects {hasObjects}");
    }

    private void UpdateFromPerception()
    {
        if (perception == null || carGPT == null) return;

        if (perception.TryGetLatest(out DetectionData data))
        {
            // Normal good data
            Vector3 rawPosition = new Vector3(-(data.roby), 0.15f, -(data.robx));
            carGPT.position = rawPosition;
            carGPT.rotation = Quaternion.Euler(0f, data.angle + 90f, 0f);

            _targetPosition = rawPosition;
            _targetHeading = (data.angle + 90f) * Mathf.Deg2Rad;

            _lastPerceptionTime = Time.time;

            if (!_positionInitialized)
            {
                _smoothedPosition = rawPosition;
                _positionInitialized = true;
            }
            if (!_headingInitialized)
            {
                _smoothedHeading = _targetHeading;
                _headingInitialized = true;
            }
        }
        else if (Time.time - _lastPerceptionTime > PERCEPTION_TIMEOUT)
        {
            Debug.LogWarning($"[AI] ⚠️ Perception stale for > {PERCEPTION_TIMEOUT}s → forcing stop & recovery");
            udpSender?.Send(new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 }, "Perception timeout stop");

            // Force the state machine to recalculate path instead of dropping off
            atTarget = false;   // <-- FIX: Do not pretend we reached the target!
            hasTarget = false;  // <-- FIX: Clear the path so it generates a new one
            pathLoaded = false;
            hasObjects = true;
            _lastPerceptionTime = Time.time; // reset timer
        }
    }

    private void ApplySmoothingInControlLoop()
    {
        // (unchanged - keep your existing SmoothDamp code here)
        _smoothedPosition = Vector3.SmoothDamp(_smoothedPosition, _targetPosition, ref _positionSmoothVelocity, PositionSmoothTime, Mathf.Infinity, Time.deltaTime);

        _smoothedHeading = SmoothDampAngle(_smoothedHeading, _targetHeading, ref _headingSmoothVelocity, HeadingSmoothTime);
    }

    // ─────────────────────────────────────────────
    // Angle-aware smoothing: takes shortest path and handles wrapping
    // ─────────────────────────────────────────────
    private float SmoothDampAngle(float current, float target, ref float velocity, float smoothTime)
    {
        // Normalize angles to [-π, π]
        float c = NormalizeAngle(current);
        float t = NormalizeAngle(target);

        // Find shortest path (might wrap around)
        float delta = AngleDelta(c, t);
        float nextTarget = c + delta;

        // Use standard SmoothDamp on the unwrapped target
        float result = Mathf.SmoothDamp(c, nextTarget, ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        
        return NormalizeAngle(result);
    }

    private float NormalizeAngle(float angle)
    {
        angle = angle % (2f * Mathf.PI);
        if (angle > Mathf.PI)
            angle -= 2f * Mathf.PI;
        else if (angle < -Mathf.PI)
            angle += 2f * Mathf.PI;
        return angle;
    }

    private float AngleDelta(float from, float to)
    {
        float delta = to - from;
        if (delta > Mathf.PI)
            delta -= 2f * Mathf.PI;
        else if (delta < -Mathf.PI)
            delta += 2f * Mathf.PI;
        return delta;
    }

    private Quaternion QuaternionFromHeading(float headingRadians)
    {
        return Quaternion.Euler(0f, headingRadians * Mathf.Rad2Deg, 0f);
    }

    // ─────────────────────────────────────────────
    // PLAYBACK MODE: Call these methods or use keyboard shortcuts
    // ─────────────────────────────────────────────

    public void PlaybackPickupZone1() => manualRecorder.StartPlayback("Pickup_Zone1");
    public void PlaybackPickupZone2() => manualRecorder.StartPlayback("Pickup_Zone2");
    public void PlaybackDropoffZone1() => manualRecorder.StartPlayback("Dropoff_Zone1");
    public void PlaybackDropoffZone2() => manualRecorder.StartPlayback("Dropoff_Zone2");

    void UpdateDebugPlayback()
    {
        // Quick playback via keyboard during test runs
        if (Input.GetKeyDown(KeyCode.F9))
        {
            RunFullDemoSequence();
        }
    }

    // ─────────────────────────────────────────────
    // FULL DEMO SEQUENCE: One button to run everything
    // ─────────────────────────────────────────────

    public void RunFullDemoSequence()
    {
        StartCoroutine(DemoSequence());
    }

    private System.Collections.IEnumerator DemoSequence()
    {
        // Clear the visited zones blacklist at start of demo
        if (areaSelector != null)
        {
            areaSelector.ResetVisitedZones();
        }
        
        Debug.Log("╔════════════════════════════════════════╗");
        Debug.Log("║   TimberBot Deterministic Demo Run     ║");
        Debug.Log("╚════════════════════════════════════════╝");

        // Cycle 1
        Debug.Log("\n[Cycle 1/2] Pickup Zone 1");
        manualRecorder.StartPlayback("Pickup_Zone1");
        yield return WaitForPlaybackComplete();

        Debug.Log("[Cycle 1/2] Dropoff Zone 1");
        manualRecorder.StartPlayback("Dropoff_Zone1");
        yield return WaitForPlaybackComplete();

        yield return new WaitForSeconds(2f);  // Pause between cycles

        // Cycle 2
        Debug.Log("\n[Cycle 2/2] Pickup Zone 2");
        manualRecorder.StartPlayback("Pickup_Zone2");
        yield return WaitForPlaybackComplete();

        Debug.Log("[Cycle 2/2] Dropoff Zone 2");
        manualRecorder.StartPlayback("Dropoff_Zone2");
        yield return WaitForPlaybackComplete();

        Debug.Log("\n╔════════════════════════════════════════╗");
        Debug.Log("║          Demo Complete! ✓              ║");
        Debug.Log("╚════════════════════════════════════════╝");
    }

    private System.Collections.IEnumerator WaitForPlaybackComplete()
    {
        while (manualRecorder.IsPlaying)
            yield return null;
    }


}