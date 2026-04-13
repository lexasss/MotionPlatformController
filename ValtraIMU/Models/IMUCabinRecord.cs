namespace ValtraIMU.Models;

internal record class Acceleration(
    /// <summary>m/s^2</summary>
    double Lateral,
    /// <summary>m/s^2</summary>
    double Longitudinal,
    /// <summary>m/s^2</summary>
    double Vertical
);

internal record class AngularVelocity2(
    /// <summary>degrees/s</summary>
    double Roll,
    /// <summary>degrees/s</summary>
    double Pitch,
    /// <summary>degrees/s</summary>
    double Yaw
) : IAngular
{
    public AngularVelocity2 ToRadians() => new(
        Roll: IAngular.Deg2Rad(Roll),
        Pitch: IAngular.Deg2Rad(Pitch),
        Yaw: IAngular.Deg2Rad(Yaw)
    );
}


internal record class Orientation2(
    /// <summary>degrees</summary>
    double Pitch,
    /// <summary>degrees</summary>
    double Roll
) : IAngular
{
    public Orientation2 ToRadians() => new(
        Pitch: IAngular.Deg2Rad(Pitch),
        Roll: IAngular.Deg2Rad(Roll)
    );
}

internal record class Vector3D(
    double X,
    double Y,
    double Z
);

internal record class FrontAxleSuspension(
    double Time,        // s
    double Voltage,     // V
    double Pressure,    // bar
    double Stroke       // %
);

internal record class IMUCabinRecord(
    long Time,

    Acceleration Acceleration,
    AngularVelocity2 AngularVelocity,
    Orientation2 Orientation,
    Vector3D Body,   // g
    Vector3D Cabin,  // g

    double CanTime,
    double GroundSpeed, // km/h
    double Rpm,
    int Torque,     // %
    Vector3D Seat,  // g
    FrontAxleSuspension FAS,
    double CabLeftRearSpringOffset, // mm

    double GpsTime,     // s
    double GpsSpeed     // km/h
) : IRecord;