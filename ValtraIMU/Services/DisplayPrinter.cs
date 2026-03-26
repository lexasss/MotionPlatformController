using MotionSystems;

namespace ValtraIMU.Services;

/// <summary>
/// Prints info into console.
/// </summary>
internal class DisplayPrinter
{
    public void Reset()
    {
        _isFirstPrint = true;
    }

    /// <summary>
    /// Prints the current status of the platform.
    /// Should be called in a loop with some delay to see the updates.
    /// </summary>
    /// <param name="info">Platform info</param>
    public void PrintStatus(ref FSMI_PlatformInfo info)
    {
        if (_isFirstPrint)
        {
            _isFirstPrint = false;
            _cursorTop = Console.CursorTop;
        }
        else
        {
            Console.CursorLeft = 0;
            Console.CursorTop = _cursorTop;
        }

        Console.WriteLine("Status. Connected: {0}, Paused: {1}",
            info.isConnected != 0 ? "yes" : "no",
            (info.state & FSMI_PlatformCurrentState.AnyPaused) != 0 ? "yes" : "no");
        Console.WriteLine("    Motor Position: {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
            info.actualMotor1_Position,
            info.actualMotor2_Position,
            info.actualMotor3_Position,
            info.actualMotor4_Position,
            info.actualMotor5_Position,
            info.actualMotor6_Position);
        Console.WriteLine("    Motor Speeds: {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
            info.actualMotor1_Speed,
            info.actualMotor2_Speed,
            info.actualMotor3_Speed,
            info.actualMotor4_Speed,
            info.actualMotor5_Speed,
            info.actualMotor6_Speed);
        Console.WriteLine("    Time: {0} ms, Worst Module: {1} (code {2})",
            info.timemark,
            info.worstModuleStatusIndex,
            info.worstModuleStatusCode);
        Console.WriteLine("    Actual Top Frame: {0}",
            info.fkRecentStatus != 0 ? "ok" : "failed");
        Console.WriteLine("          Position: heave {0,-8:N4} surge {1,-8:N4}  sway {2,-8:N4}",
            info.fkHeave,
            info.fkSurge,
            info.fkSway);
        Console.WriteLine("          Rotation: roll {0,-8:N1}  pitch {1,-8:N1}  yaw {2,-8:N1}",
            info.fkRoll_deg,
            info.fkPitch_deg,
            info.fkYaw_deg);
        Console.WriteLine("    Velocity, mm/s and deg/s");
        Console.WriteLine("          Position: heave {0,-8:N2} surge {1,-8:N2}  sway {2,-8:N2}",
            info.fkHeaveSpeed,
            info.fkSurgeSpeed,
            info.fkSwaySpeed);
        Console.WriteLine("          Rotation: roll {0,-8:N1}  pitch {1,-8:N1}  yaw {2,-8:N1}",
            info.fkRollSpeed_deg,
            info.fkPitchSpeed_deg,
            info.fkYawSpeed_deg);
        Console.WriteLine("    Acceleration, mm/s^2 and deg/s^2");
        Console.WriteLine("          Position: heave {0,-8:N2} surge {1,-8:N2}  sway {2,-8:N2}",
            info.fkHeaveAcc,
            info.fkSurgeAcc,
            info.fkSwayAcc);
        Console.WriteLine("          Rotation: roll {0,-8:N1}  pitch {1,-8:N1}  yaw {2,-8:N1}",
            info.fkRollAcc_deg,
            info.fkPitchAcc_deg,
            info.fkYawAcc_deg);
    }

    #region Internal

    bool _isFirstPrint = true;
    int _cursorTop;

    #endregion
}
