using MotionSystems;
using Spectre.Console;

namespace ValtraMPC.Services;

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
        if (!_isFirstPrint)
        {
            Console.CursorLeft = 0;
            Console.CursorTop = _cursorTop;
        }

        List<string> lines = [];
        lines.Add(string.Format("[yellow]Time[/] {0} ms. [yellow]Worst Module[/] {1} (code {2})",
            info.timemark,
            info.worstModuleStatusIndex,
            info.worstModuleStatusCode));
        lines.Add(string.Format("Status: [yellow]Cooling system[/] {0}, [yellow]Termal protection[/] {1} [yellow]FK[/] {2}, [yellow]IK[/] {3}",
            info.coolingSystemMalfunction,
            info.isThermalProtectionActivated,
            info.fkRecentStatus,
            info.ikRecentStatus));
        lines.Add(string.Format("[yellow]Connected[/] {0}, [yellow]Paused[/] {1}, [yellow]Ref-Run completed[/] {2}, [yellow]Parked[/] {3}/{4}",
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
            }));
        lines.Add(string.Format("[yellow]Motor Position[/] {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
            info.actualMotor1_Position,
            info.actualMotor2_Position,
            info.actualMotor3_Position,
            info.actualMotor4_Position,
            info.actualMotor5_Position,
            info.actualMotor6_Position));
        lines.Add(string.Format("[yellow]Motor Speeds[/] {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
            info.actualMotor1_Speed,
            info.actualMotor2_Speed,
            info.actualMotor3_Speed,
            info.actualMotor4_Speed,
            info.actualMotor5_Speed,
            info.actualMotor6_Speed));
        lines.Add(string.Format("[yellow]Actual Top Frame[/] {0}",
            info.fkRecentStatus != 0 ? "ok" : "failed"));
        lines.Add(string.Format("    Position: heave {0,8:N4}   surge {1,8:N4}   sway {2,8:N4}",
            info.fkHeave,
            info.fkSurge,
            info.fkSway));
        lines.Add(string.Format("    Rotation:  roll {0,8:N1}   pitch {1,8:N1}    yaw {2,8:N1}",
            info.fkRoll_deg,
            info.fkPitch_deg,
            info.fkYaw_deg));
        lines.Add("[yellow]Velocity[/] mm/s and deg/s");
        lines.Add(string.Format("    Position: heave {0,8:N2}   surge {1,8:N2}   sway {2,8:N2}",
            info.fkHeaveSpeed,
            info.fkSurgeSpeed,
            info.fkSwaySpeed));
        lines.Add(string.Format("    Rotation:  roll {0,8:N1}   pitch {1,8:N1}    yaw {2,8:N1}",
            info.fkRollSpeed_deg,
            info.fkPitchSpeed_deg,
            info.fkYawSpeed_deg));
        lines.Add("[yellow]Acceleration[/] mm/s^2 and deg/s^2");
        lines.Add(string.Format("    Position: heave {0,8:N2}   surge {1,8:N2}   sway {2,8:N2}",
            info.fkHeaveAcc,
            info.fkSurgeAcc,
            info.fkSwayAcc));
        lines.Add(string.Format("    Rotation:  roll {0,8:N1}   pitch {1,8:N1}    yaw {2,8:N1}",
            info.fkRollAcc_deg,
            info.fkPitchAcc_deg,
            info.fkYawAcc_deg));

        foreach (var line in lines)
            AnsiConsole.MarkupLine(line);

        if (_isFirstPrint)
        {
            _isFirstPrint = false;
            _cursorTop = Console.CursorTop - lines.Count;
        }
    }

    #region Internal

    bool _isFirstPrint = true;
    int _cursorTop;

    #endregion
}
