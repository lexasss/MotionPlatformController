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
        }
        else
        {
            Console.CursorLeft = 0;
            Console.CursorTop = Math.Max(0, Console.CursorTop - 6);
        }

        Console.WriteLine("Status. Connected: {0}, Paused: {1}",
            info.isConnected != 0 ? "yes" : "no",
            (info.state & FSMI_PlatformCurrentState.AnyPaused) != 0 ? "yes" : "no");
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
        Console.WriteLine("          roll {0,-8:N4}  pitch {1,-8:N4}  yaw {2,-8:N4}",
            info.fkRoll,
            info.fkPitch,
            info.fkYaw);
        Console.WriteLine("          heave {0,-8:N4} surge {1,-8:N4}  sway {2,-8:N4}",
            info.fkHeave,
            info.fkSurge,
            info.fkSway);
    }

    #region Internal

    bool _isFirstPrint = true;

    #endregion
}
