using MotionSystems;
using System.Runtime.InteropServices;

namespace ValtraIMU.Services;

internal class StatusPrinter
{
    public StatusPrinter()
    {
        _piSize = (uint)Marshal.SizeOf(_platformInfo);
    }

    public void Reset()
    {
        _recentMark = 0;
        _isFirstPrint = true;
    }

    public bool PrintStatus(ForceSeatMI_NET8 mi)
    {
        // Get current status
        if (mi.GetPlatformInfoEx(ref _platformInfo, _piSize, 100))
        {
            if (_platformInfo.structSize != Marshal.SizeOf(_platformInfo))
            {
                Console.WriteLine("Incorrect structure size: {0} vs {1}", _platformInfo.structSize, Marshal.SizeOf(_platformInfo));
                return false;
            }
            else if (_platformInfo.timemark == _recentMark)
            {
                //Console.WriteLine("No new platform info");
            }
            else
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
                    _platformInfo.isConnected != 0 ? "yes" : "no",
                    (_platformInfo.state & FSMI_PlatformCurrentState.AnyPaused) != 0 ? "yes" : "no");
                Console.WriteLine("    Actual Motor Positions: {0,8}, {1,8}, {2,8}, {3,8}, {4,8}, {5,8}",
                    _platformInfo.actualMotor1_Position,
                    _platformInfo.actualMotor2_Position,
                    _platformInfo.actualMotor3_Position,
                    _platformInfo.actualMotor4_Position,
                    _platformInfo.actualMotor5_Position,
                    _platformInfo.actualMotor6_Position);
                Console.WriteLine("    Time: {0} ms, Worst Module: {1} (code {2})",
                    _platformInfo.timemark,
                    _platformInfo.worstModuleStatusIndex,
                    _platformInfo.worstModuleStatusCode);
                Console.WriteLine("    Actual Top Frame: {0}",
                    _platformInfo.fkRecentStatus != 0 ? "ok" : "failed");
                Console.WriteLine("          roll {0,-8:N4}  pitch {1,-8:N4}  yaw {2,-8:N4}",
                    _platformInfo.fkRoll,
                    _platformInfo.fkPitch,
                    _platformInfo.fkYaw);
                Console.WriteLine("          heave {0,-8:N4} surge {1,-8:N4}  sway {2,-8:N4}",
                    _platformInfo.fkHeave,
                    _platformInfo.fkSurge,
                    _platformInfo.fkSway);

                _recentMark = _platformInfo.timemark;
            }
        }
        else
        {
            Console.WriteLine("Failed to get platform info");
        }

        return true;
    }

    // Internal

    FSMI_PlatformInfo _platformInfo = new();
    uint _piSize;
    ulong _recentMark = 0;
    bool _isFirstPrint = true;
}
