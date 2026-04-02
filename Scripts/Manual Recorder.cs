using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Comprehensive manual recording/playback system.
/// Records UDP packets (VelocityOutput) to JSON files in a persistent directory.
/// Label sections by zone/task. Record once, replay anywhere.
/// 
/// Recordings stored in: Application.persistentDataPath/Recordings/
/// File format: {label}.json (e.g., "Pickup_Zone2.json")
/// </summary>
public class ManualRecorder : MonoBehaviour
{
    [System.Serializable]
    public struct RecordedFrame
    {
        public float timestamp;
        public float vx;
        public float vy;
        public float omega;
        public float stepper;
    }

    [System.Serializable]
    public class RecordedSection
    {
        public string label;
        public float totalDuration;
        public int frameCount;
        public List<RecordedFrame> frames = new List<RecordedFrame>();
    }

    // ─────────────────────────────────────────────
    // CONFIG
    // ─────────────────────────────────────────────
    private string _recordingsDir;    
    
    // ─────────────────────────────────────────────
    // RECORDING STATE
    // ─────────────────────────────────────────────
    private bool _isRecording = false;
    private string _currentSectionLabel = "";
    private List<RecordedFrame> _currentFrames = new List<RecordedFrame>();
    private float _recordingStartTime = 0f;
    private int _recordedFrameCount = 0;

    // ─────────────────────────────────────────────
    // PLAYBACK STATE
    // ─────────────────────────────────────────────
    private bool _isPlaying = false;
    private RecordedSection _playbackSection = null;
    private float _playbackStartTime = 0f;

    // Make sure to set IsPlaying = true inside StartPlayback() 
    // and IsPlaying = false when playback finishes.

    // ─────────────────────────────────────────────
    // CACHE: in-memory loaded sections
    // ─────────────────────────────────────────────
    private Dictionary<string, RecordedSection> _loadedSections = new Dictionary<string, RecordedSection>();

    void Awake()
    {
        // Assign the path now that Unity is actually running
        _recordingsDir = System.IO.Path.Combine(Application.persistentDataPath, "Recordings");

        // Create the folder if it doesn't exist
        if (!System.IO.Directory.Exists(_recordingsDir))
        {
            System.IO.Directory.CreateDirectory(_recordingsDir);
        }
    }

    // ─────────────────────────────────────────────
    // FILE I/O
    // ─────────────────────────────────────────────

    private void EnsureRecordingsDirectory()
    {
        if (!Directory.Exists(_recordingsDir))
        {
            Directory.CreateDirectory(_recordingsDir);
            Debug.Log($"[ManualRecorder] Created recordings directory: {_recordingsDir}");
        }
    }

    private string GetFilePath(string label)
    {
        // Sanitize label for filename
        string sanitized = System.Text.RegularExpressions.Regex.Replace(label, @"[^\w\-_]", "_");
        return Path.Combine(_recordingsDir, $"{sanitized}.json");
    }

    // ─────────────────────────────────────────────
    // RECORDING API
    // ─────────────────────────────────────────────

    /// <summary>Start recording a labeled section (e.g., "Pickup_Zone2")</summary>
    public void StartRecording(string label)
    {
        if (_isRecording)
        {
            Debug.LogWarning($"[ManualRecorder] Already recording '{_currentSectionLabel}'. Call EndRecording() first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            Debug.LogError("[ManualRecorder] Label cannot be empty.");
            return;
        }

        _isRecording = true;
        _currentSectionLabel = label;
        _currentFrames.Clear();
        _recordingStartTime = Time.time;
        _recordedFrameCount = 0;

        Debug.Log($"[ManualRecorder] ▶ Recording '{label}'...");
    }

    /// <summary>Capture a UDP packet during manual override</summary>
    public void RecordPacket(VelocityOutput velocity)
    {
        if (!_isRecording) return;

        float relativeTime = Time.time - _recordingStartTime;
        _currentFrames.Add(new RecordedFrame
        {
            timestamp = relativeTime,
            vx = velocity.vx,
            vy = velocity.vy,
            omega = velocity.omega,
            stepper = velocity.stepper
        });

        _recordedFrameCount++;
    }

    /// <summary>Stop recording and save to JSON file</summary>
    public bool EndRecording()
    {
        if (!_isRecording)
        {
            Debug.LogWarning("[ManualRecorder] Not currently recording.");
            return false;
        }

        _isRecording = false;

        if (_currentFrames.Count == 0)
        {
            Debug.LogWarning($"[ManualRecorder] Section '{_currentSectionLabel}' has no frames. Discarded.");
            return false;
        }

        float totalDuration = _currentFrames.Count > 0
            ? _currentFrames[_currentFrames.Count - 1].timestamp
            : 0f;

        var section = new RecordedSection
        {
            label = _currentSectionLabel,
            totalDuration = totalDuration,
            frameCount = _currentFrames.Count,
            frames = new List<RecordedFrame>(_currentFrames)
        };

        // Save to JSON
        string filePath = GetFilePath(_currentSectionLabel);
        string json = JsonUtility.ToJson(section, true);

        try
        {
            File.WriteAllText(filePath, json);
            _loadedSections[_currentSectionLabel] = section; // Cache it
            Debug.Log($"[ManualRecorder] ✓ Saved '{_currentSectionLabel}' → {filePath}");
            Debug.Log($"  Frames: {_currentFrames.Count} | Duration: {totalDuration:F3}s");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ManualRecorder] Failed to save '{_currentSectionLabel}': {e.Message}");
            return false;
        }

        _currentSectionLabel = "";
        _currentFrames.Clear();
        return true;
    }

    // ─────────────────────────────────────────────
    // PLAYBACK API
    // ─────────────────────────────────────────────

    /// <summary>Load a recorded section from disk (if not already cached)</summary>
    public bool LoadSection(string label)
    {
        // Already cached?
        if (_loadedSections.ContainsKey(label))
            return true;

        string filePath = GetFilePath(label);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[ManualRecorder] Recording '{label}' not found at {filePath}");
            ListAvailableRecordings();
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            RecordedSection section = JsonUtility.FromJson<RecordedSection>(json);
            _loadedSections[label] = section;
            Debug.Log($"[ManualRecorder] ✓ Loaded '{label}' ({section.frameCount} frames, {section.totalDuration:F3}s)");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ManualRecorder] Failed to load '{label}': {e.Message}");
            return false;
        }
    }

    /// <summary>Start replaying a recorded section</summary>
    public bool StartPlayback(string label)
    {
        // Load if not cached
        if (!LoadSection(label))
            return false;

        if (_isPlaying)
        {
            Debug.LogWarning($"[ManualRecorder] Already playing '{_playbackSection.label}'. Call StopPlayback() first.");
            return false;
        }

        _playbackSection = _loadedSections[label];
        _playbackStartTime = Time.time;
        _isPlaying = true;

        Debug.Log($"[ManualRecorder] ▶ Playing '{label}' ({_playbackSection.frames.Count} frames, {_playbackSection.totalDuration:F3}s)...");
        return true;
    }

    /// <summary>Get the next velocity output during playback (returns zero output if done or not playing)</summary>
    public VelocityOutput GetPlaybackVelocity()
    {
        if (!_isPlaying || _playbackSection == null)
            return new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 };

        float elapsedTime = Time.time - _playbackStartTime;

        // If we've exceeded total duration, stop playback
        if (elapsedTime >= _playbackSection.totalDuration)
        {
            Debug.Log($"[ManualRecorder] ✓ Playback of '{_playbackSection.label}' complete.");
            StopPlayback();
            return new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 };
        }

        // Find the frame that matches the current elapsed time
        RecordedFrame frame = _playbackSection.frames[0];
        for (int i = 0; i < _playbackSection.frames.Count; i++)
        {
            if (_playbackSection.frames[i].timestamp <= elapsedTime)
            {
                frame = _playbackSection.frames[i];
            }
            else
            {
                break;
            }
        }

        return new VelocityOutput
        {
            vx = frame.vx,
            vy = frame.vy,
            omega = frame.omega,
            stepper = frame.stepper
        };
    }

    /// <summary>Stop playback and clear state</summary>
    public void StopPlayback()
    {
        _isPlaying = false;
        _playbackSection = null;
    }

    /// <summary>Check if playback is currently active</summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>Check if recording is currently active</summary>
    public bool IsRecording => _isRecording;

    // ─────────────────────────────────────────────
    // INSPECTION / DEBUG
    // ─────────────────────────────────────────────

    /// <summary>List all recorded sections on disk</summary>
    public List<string> GetAvailableRecordings()
    {
        EnsureRecordingsDirectory();

        var files = Directory.GetFiles(_recordingsDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        return files;
    }

    /// <summary>Print summary of all recordings</summary>
    public void ListAvailableRecordings()
    {
        var recordings = GetAvailableRecordings();

        Debug.Log("=== Available Recordings ===");
        if (recordings.Count == 0)
        {
            Debug.Log("  (none found)");
            return;
        }

        foreach (string label in recordings)
        {
            if (LoadSection(label))
            {
                var section = _loadedSections[label];
                Debug.Log($"  [{label}] {section.frameCount} frames | {section.totalDuration:F3}s");
            }
        }

        Debug.Log($"Directory: {_recordingsDir}");
    }

    /// <summary>Print detailed info about a specific recording</summary>
    public void InspectRecording(string label)
    {
        if (!LoadSection(label))
            return;

        var section = _loadedSections[label];
        Debug.Log($"=== Recording: {label} ===");
        Debug.Log($"  Frames: {section.frameCount}");
        Debug.Log($"  Duration: {section.totalDuration:F3}s");
        Debug.Log($"  File: {GetFilePath(label)}");

        // Sample first, middle, last frames
        if (section.frames.Count > 0)
        {
            Debug.Log($"  First frame: vx={section.frames[0].vx:F2}, vy={section.frames[0].vy:F2}, omega={section.frames[0].omega:F2}, stepper={section.frames[0].stepper:F2}");
            if (section.frames.Count > 1)
            {
                int mid = section.frames.Count / 2;
                Debug.Log($"  Mid frame:   vx={section.frames[mid].vx:F2}, vy={section.frames[mid].vy:F2}, omega={section.frames[mid].omega:F2}, stepper={section.frames[mid].stepper:F2}");
                Debug.Log($"  Last frame:  vx={section.frames[section.frames.Count - 1].vx:F2}, vy={section.frames[section.frames.Count - 1].vy:F2}, omega={section.frames[section.frames.Count - 1].omega:F2}, stepper={section.frames[section.frames.Count - 1].stepper:F2}");
            }
        }
    }

    /// <summary>Delete a recorded section from disk</summary>
    public bool DeleteRecording(string label)
    {
        string filePath = GetFilePath(label);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _loadedSections.Remove(label);
                Debug.Log($"[ManualRecorder] Deleted recording '{label}'");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ManualRecorder] Failed to delete '{label}': {e.Message}");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[ManualRecorder] Recording '{label}' not found.");
            return false;
        }
    }

    /// <summary>Get the directory where recordings are stored</summary>
    public string GetRecordingsDirectory() => _recordingsDir;

    /// <summary>Export all recordings as a CSV summary (for analysis)</summary>
    public void ExportSummaryCSV()
    {
        var recordings = GetAvailableRecordings();
        var lines = new List<string> { "Label,Frames,Duration_s,File" };

        foreach (string label in recordings)
        {
            if (LoadSection(label))
            {
                var section = _loadedSections[label];
                lines.Add($"{label},{section.frameCount},{section.totalDuration:F3},{GetFilePath(label)}");
            }
        }

        string csvPath = Path.Combine(_recordingsDir, "_SUMMARY.csv");
        File.WriteAllLines(csvPath, lines);
        Debug.Log($"[ManualRecorder] Exported summary to {csvPath}");
    }

    /// <summary>Clear all recordings and cache</summary>
    public void ClearAll()
    {
        var recordings = GetAvailableRecordings();
        foreach (string label in recordings)
        {
            DeleteRecording(label);
        }
        _loadedSections.Clear();
        Debug.Log("[ManualRecorder] All recordings cleared.");
    }
}