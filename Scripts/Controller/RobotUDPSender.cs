using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

// vx/vy/omega arrive normalized [-1, 1] from Unity physics.
// Pico expects [-100, 100]. Multiply by 100, clamp, done.
// No speed scaling — the Pico's STEPPER_MAX_SPEED is the real ceiling.

public class RobotUDPSender : MonoBehaviour
{
    [Header("Network")]
    public string targetIP   = "127.0.0.1";
    public int    targetPort = 5005;

    [Header("Recording")]
    public ManualRecorder manualRecorder;

    private UdpClient  _udp;
    private IPEndPoint _endpoint;

    public string senderName = "UnnamedSender";
    void Awake()
    {
        _udp      = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
    }

    public void Send(VelocityOutput v, string senderName)
    {

        Debug.Log($"[UDP] Sent by: {senderName} | vx:{v.vx} vy:{v.vy} omega:{v.omega}");

        float vx      = Mathf.Clamp(v.vx    * 100f, -100f, 100f);
        float vy      = Mathf.Clamp(v.vy    * 100f, -100f, 100f);
        float omega   = Mathf.Clamp(v.omega * 100f, -100f, 100f);
        float stepper = Mathf.Clamp(v.stepper,       -100f, 100f);

        var    ic   = System.Globalization.CultureInfo.InvariantCulture;
        string json = "{\"type\":\"velocity\",\"vx\":"  + vx.ToString("F2", ic)
                    + ",\"vy\":"                        + vy.ToString("F2", ic)
                    + ",\"omega\":"                     + omega.ToString("F2", ic)
                    + ",\"stepper\":"                   + stepper.ToString("F2", ic) + "}\n";

        byte[] data = Encoding.UTF8.GetBytes(json);
        try   { _udp.Send(data, data.Length, _endpoint); }
        catch (System.Exception e) { Debug.LogWarning($"[UDP] Send failed: {e.Message}"); }

        // ─── RECORDING: Capture packet if recording is active ───
        if (manualRecorder != null && manualRecorder.IsRecording)
        {
            manualRecorder.RecordPacket(v);
        }
    }

    void OnDestroy()
    {
        Send(new VelocityOutput { vx = 0, vy = 0, omega = 0, stepper = 0 }, "onDestory");
        _udp?.Close();
    }
}