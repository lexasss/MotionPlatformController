namespace ValtraIMU.Models;

internal interface IAngular
{
    protected static double Deg2Rad(double degrees) => degrees * Math.PI / 180;
}

internal record class Coordinates(
    /// <summary>degress</summary>
    double Latitude,
    /// <summary>degress</summary>
    double Longitude,
    /// <summary>meters</summary>
    double Elevation
);

internal record class Position(
    /// <summary>meters</summary>
    double Easting,
    /// <summary>meters</summary>
    double Northing);

internal record class Orientation(
    /// <summary>degrees</summary>
    double Roll,
    /// <summary>degrees</summary>
    double Pitch,
    /// <summary>degrees</summary>
    double Heading
) : IAngular
{
    public Orientation ToRadians() => new(
        Roll: IAngular.Deg2Rad(Roll),
        Pitch: IAngular.Deg2Rad(Pitch),
        Heading: IAngular.Deg2Rad(Heading)
    );
}

internal record class Velocity(
    /// <summary>m/s</summary>
    double East,
    /// <summary>m/s</summary>
    double North,
    /// <summary>m/s</summary>
    double Up
);

internal record class AbsoluteAcceleration(
    /// <summary>m/s^2</summary>
    double East,
    /// <summary>m/s^2</summary>
    double North,
    /// <summary>m/s^2</summary>
    double Up);

internal record class BodyAcceleration(
    /// <summary>m/s^2</summary>
    double X,
    /// <summary>m/s^2</summary>
    double Y,
    /// <summary>m/s^2</summary>
    double Z
);

internal record class AngularVelocity(
    /// <summary>degrees/s</summary>
    double X,
    /// <summary>degrees/s</summary>
    double Y,
    /// <summary>degrees/s</summary>
    double Z
) : IAngular
{
    public AngularVelocity ToRadians() => new(
        X: IAngular.Deg2Rad(X),
        Y: IAngular.Deg2Rad(Y),
        Z: IAngular.Deg2Rad(Z)
    );
}

internal record class IMUFrontRecord(
    int Week,
    long Time,
    Coordinates Coordinates,
    Position Position,
    Orientation Orientation,
    Velocity Velocity,
    AbsoluteAcceleration AbsoluteAcceleration,
    BodyAcceleration BodyAcceleration,
    AngularVelocity AngularVelocity
) : IRecord;
