using UnityEngine;

[System.Serializable]
public class ControlConfig
{
    [Header("Position Control")]
    public float positionGain = 2.0f;

    [Header("Heading Control")]
    public float headingGain = 4.0f;

    [Header("Goal Behaviour")]
    public float goalOrientationThreshold = 0.5f;

    [Header("Lookahead Distance (meters)")]
    public float lookaheadDistance = 0.5f;
}

