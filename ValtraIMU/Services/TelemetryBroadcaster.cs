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

        Send(DateTime.Now.Ticks,                            // time
            0, 0, 0,                                        // position
            telemetry.bodyRoll, telemetry.bodyPitch, 0,     // rotation
            linVel.forward, linVel.right, linVel.upward,    // linear velocity
            angVel.pitch, angVel.yaw, angVel.roll,          // angular velocity
            linAcc.forward, linAcc.right, linAcc.upward,    // linear acceleration
            0, 0, 0                                         // angular acceleration
        );
    }

    public void Send(ref FSMI_PlatformInfo info)
    {
        Send(info.timemark,                                                             // time
            info.fkSurge / 1000, info.fkSway / 1000, info.fkHeave / 1000,               // position
            info.fkRoll_deg, info.fkPitch_deg, info.fkYaw_deg,                          // rotation
            info.fkSurgeSpeed / 1000, info.fkSwaySpeed / 1000, info.fkHeaveSpeed / 1000,// linear velocity
            info.fkRollSpeed_deg, info.fkPitchSpeed_deg, info.fkYawSpeed_deg,           // angular velocity
            info.fkSurgeAcc / 1000, info.fkSwayAcc / 1000, info.fkHeaveAcc / 1000,      // linear acceleration
            info.fkRollAcc_deg, info.fkPitchAcc_deg, info.fkYawAcc_deg                  // angular acceleration
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
