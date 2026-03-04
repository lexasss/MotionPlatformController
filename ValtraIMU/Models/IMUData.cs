namespace ValtraIMU.Models;

internal record class Coordinates(
    /// <summary>degress</summary>
    float Latitude,
    /// <summary>degress</summary>
    float Longitude,
    /// <summary>meters</summary>
    float Elevation
);

internal record class Position(
    /// <summary>meters</summary>
    float Easting,
    /// <summary>meters</summary>
    float Northing);

internal record class Orientation(
    /// <summary>degrees</summary>
    float Roll,
    /// <summary>degrees</summary>
    float Pitch,
    /// <summary>degrees</summary>
    float Heading
);

internal record class Velocity(
    /// <summary>m/s</summary>
    float East,
    /// <summary>m/s</summary>
    float North,
    /// <summary>m/s</summary>
    float Up
);

internal record class AbsoluteAcceleration(
    /// <summary>m/s^2</summary>
    float East,
    /// <summary>m/s^2</summary>
    float North,
    /// <summary>m/s^2</summary>
    float Up);

internal record class BodyAcceleration(
    /// <summary>m/s^2</summary>
    float X,
    /// <summary>m/s^2</summary>
    float Y,
    /// <summary>m/s^2</summary>
    float Z
);

internal record class AngularVelocity(
    /// <summary>degrees/s</summary>
    float X,
    /// <summary>degrees/s</summary>
    float Y,
    /// <summary>degrees/s</summary>
    float Z
);

internal record class IMUData(
    float Week,
    float GPSTime,
    Coordinates Coordinates,
    Position Position,
    Orientation Orientation,
    Velocity Velocity,
    AbsoluteAcceleration AbsoluteAcceleration,
    BodyAcceleration BodyAcceleration,
    AngularVelocity AngularVelocity
);
