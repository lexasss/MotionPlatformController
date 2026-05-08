using MotionSystems;
using Spectre.Console;

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
            _cursorTop = Console.CursorTop;
        }
        else
        {
            Console.CursorLeft = 0;
            Console.CursorTop = _cursorTop;
        }

        AnsiConsole.MarkupLine("[yellow]Status[/]. Connected: [bold]{0}[/], Paused: [bold]{1}[/], Ref-Run completed: [bold]{2}[/], Parked: [bold]{3}/{4}[/]",
            info.isConnected != 0 ? "yes" : "no",
            (info.state & FSMI_PlatformCurrentState.AnyPaused) != 0 ? "yes" : "no",
            (info.state & FSMI_PlatformCurrentState.RefRunCompleted) != 0 ? "yes" : "no",
            (info.state & FSMI_PlatformCurrentState.ParkingCompleted) != 0 ? "yes" : "no",
            (info.state >> 5) switch
            {
                1 => "Center (soft)",
                2 => "Normal (soft)",
                3 => "Transp (soft)",
                4 => "Center",
                5 => "Normal",
                6 => "Transp",
                _ => "-"
            });
        AnsiConsole.MarkupLine("[yellow]Motor Position[/] {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
            info.actualMotor1_Position,
            info.actualMotor2_Position,
            info.actualMotor3_Position,
            info.actualMotor4_Position,
            info.actualMotor5_Position,
            info.actualMotor6_Position);
        AnsiConsole.MarkupLine("[yellow]Motor Speeds[/] {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
            info.actualMotor1_Speed,
            info.actualMotor2_Speed,
            info.actualMotor3_Speed,
            info.actualMotor4_Speed,
            info.actualMotor5_Speed,
            info.actualMotor6_Speed);
        AnsiConsole.MarkupLine("[yellow]Time[/] {0} ms, [yellow]Worst Module[/] {1} (code {2})",
            info.timemark,
            info.worstModuleStatusIndex,
            info.worstModuleStatusCode);
        AnsiConsole.MarkupLine("[yellow]Actual Top Frame[/] {0}",
            info.fkRecentStatus != 0 ? "ok" : "failed");
        AnsiConsole.MarkupLine("    Position: heave {0,-8:N4} surge {1,-8:N4}  sway {2,-8:N4}",
            info.fkHeave,
            info.fkSurge,
            info.fkSway);
        AnsiConsole.MarkupLine("    Rotation: roll {0,-8:N1}  pitch {1,-8:N1}  yaw {2,-8:N1}",
            info.fkRoll_deg,
            info.fkPitch_deg,
            info.fkYaw_deg);
        AnsiConsole.MarkupLine("[yellow]Velocity[/] mm/s and deg/s");
        AnsiConsole.MarkupLine("    Position: heave {0,-8:N2} surge {1,-8:N2}  sway {2,-8:N2}",
            info.fkHeaveSpeed,
            info.fkSurgeSpeed,
            info.fkSwaySpeed);
        AnsiConsole.MarkupLine("    Rotation: roll {0,-8:N1}  pitch {1,-8:N1}  yaw {2,-8:N1}",
            info.fkRollSpeed_deg,
            info.fkPitchSpeed_deg,
            info.fkYawSpeed_deg);
        AnsiConsole.MarkupLine("[yellow]Acceleration[/] mm/s^2 and deg/s^2");
        AnsiConsole.MarkupLine("    Position: heave {0,-8:N2} surge {1,-8:N2}  sway {2,-8:N2}",
            info.fkHeaveAcc,
            info.fkSurgeAcc,
            info.fkSwayAcc);
        AnsiConsole.MarkupLine("    Rotation: roll {0,-8:N1}  pitch {1,-8:N1}  yaw {2,-8:N1}",
            info.fkRollAcc_deg,
            info.fkPitchAcc_deg,
            info.fkYawAcc_deg);

        if (_isFirstPrint)
        {
            _isFirstPrint = false;
            if (Console.CursorTop - _cursorTop < 13)    // the window was scrolled
            {
                _cursorTop = Console.CursorTop - 13;
            }
        }
    }

    #region Internal

    bool _isFirstPrint = true;
    int _cursorTop;

    #endregion
}
