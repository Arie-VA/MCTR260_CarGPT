using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

[System.Serializable]
public class DetectionData
    {
        public float angle;
        public float robx;
        public float roby;
    }


public class Perception : MonoBehaviour
{
    private DetectionData latestData;
    private bool hasNewData = false;

    private readonly object dataLock = new object();

    void Start()
    {
        ROSConnection.GetOrCreateInstance()
            .Subscribe<StringMsg>("/arena/detections", ProcessDetection);
    }

    void ProcessDetection(StringMsg msg)
    {
        DetectionData data = JsonUtility.FromJson<DetectionData>(msg.data);

        if (data == null) return;

        lock (dataLock)
        {
            latestData = data;
            hasNewData = true;
        }
    }

    public bool TryGetLatest(out DetectionData data)
    {
        lock (dataLock)
        {
            if (!hasNewData)
            {
                data = null;
                return false;
            }

            data = latestData;
            hasNewData = false;
            return true;
        }
    }
}