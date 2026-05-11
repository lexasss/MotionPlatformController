using MotionSystems;
using Spectre.Console;

namespace ValtraMPC.TelemetryConfigers;

internal class Orientation : ITelemetryConfiger
{
    public void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] value)
    {
        if (settings.IsVerbose && !settings.IsDebugMode)
        {
            Console.CursorLeft = 0;
            AnsiConsole.Write($"BodyPitch: {value[0]:F2} deg, BodyRoll: {value[1]:F2} deg");
        }

        telemetry.bodyPitch = (float)(value[0] / 180 * Math.PI);
        telemetry.bodyRoll = (float)(value[1] / 180 * Math.PI);
    }
}
