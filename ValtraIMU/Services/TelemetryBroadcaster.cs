using MotionSystems;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ValtraIMU.Services;

internal class TelemetryBroadcaster
{
    public TelemetryBroadcaster()
    {
        _client = new UdpClient();
        _client.EnableBroadcast = true;

        _endPoint = new IPEndPoint(IPAddress.Loopback, 38777);
    }

    public void Send(ref FSMI_TelemetryACE telemetry)
    {
        ref var linVel = ref telemetry.bodyLinearVelocity[0];
        ref var linAcc = ref telemetry.bodyLinearAcceleration[0];
        ref var angVel = ref telemetry.bodyAngularVelocity[0];

        Send(0, DateTime.Now.Ticks,                         // time
            0, 0, 0, 0, 0,      // location
            telemetry.bodyRoll, telemetry.bodyPitch, 0,     // rotation
            linVel.forward, linVel.right, linVel.upward,    // linear velocity
            0, 0, 0,            // earth-based acceleration
            linAcc.forward, linAcc.right, linAcc.upward,    // body acceleration
            angVel.pitch, angVel.yaw, angVel.roll           // angular velocity
        );
    }

    public void Send(params float[] values)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(values.AsSpan());
        _client.Send(bytes.ToArray(), bytes.Length, _endPoint);
    }

    // Internal

    readonly UdpClient _client;
    readonly IPEndPoint _endPoint;
}
