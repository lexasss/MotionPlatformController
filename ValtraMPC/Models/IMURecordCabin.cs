using Spectre.Console;
using System.Text.RegularExpressions;

namespace ValtraMPC.Models;

internal record class Acceleration(
    /// <summary>m/s^2</summary>
    double Lateral,
    /// <summary>m/s^2</summary>
    double Longitudinal,
    /// <summary>m/s^2</summary>
    double Vertical
)
{
    public double[] ToArray() => [Lateral, Longitudinal, Vertical];
    public static Acceleration FromArray(double[] arr) => new(
        Lateral: arr[0],
        Longitudinal: arr[1],
        Vertical: arr[2]
    );
};

internal record class AngularVelocity(
    /// <summary>degrees/s</summary>
    double Roll,
    /// <summary>degrees/s</summary>
    double Pitch,
    /// <summary>degrees/s</summary>
    double Yaw
) : IAngular
{
    public AngularVelocity ToRadians() => new(
        Roll: IAngular.Deg2Rad(Roll),
        Pitch: IAngular.Deg2Rad(Pitch),
        Yaw: IAngular.Deg2Rad(Yaw)
    );
}

internal record class FrontAxleSuspension(
    double Time,        // s
    double Voltage,     // V
    double Pressure,    // bar
    double Stroke       // %
);

internal record class IMURecordCabin(
    long Timestamp,

    Acceleration Acceleration,
    AngularVelocity AngularVelocity,
    Orientation Orientation,    // Heading is always 0
    Vector3D Body,      // g
    Vector3D Cabin,     // g

    double CanTime,
    double GroundSpeed, // km/h
    double Rpm,
    int Torque,         // %
    Vector3D Seat,      // g
    FrontAxleSuspension FAS,
    double CabLeftRearSpringOffset, // mm

    double GpsTime,     // s
    double GpsSpeed     // km/h
) : IRecord;
