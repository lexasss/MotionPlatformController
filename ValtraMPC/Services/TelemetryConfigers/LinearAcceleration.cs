using MotionSystems;
using Spectre.Console;

namespace ValtraMPC.TelemetryConfigers;

internal class LinearAcceleration : ITelemetryConfiger
{
    public void Config(ref FSMI_TelemetryACE telemetry, Settings settings, double[] values)
    {
        if (settings.IsVerbose && !settings.IsDebugMode)
        {
            Console.CursorLeft = 0;
            var valuesStr = string.Join(' ', values.Select(value => $"{value:F4} m/s²"));
            AnsiConsole.Write($"LinearAcceleleration: {valuesStr}");
        }

        ref FSMI_TelemetryRUF linAccel = ref telemetry.bodyLinearAcceleration[0];
        if (values.Length == 1)
        {
            switch (settings.Axis.Value)
            {
                case Axis.Right:
                    linAccel.right = (float)values[0];
                    break;
                case Axis.Forward:
                    linAccel.forward = (float)values[0];
                    break;
                case Axis.Upward:
                    linAccel.upward = (float)values[0];
                    break;
            }
        }
        else if (values.Length == 2)
        {
            if (settings.SimulationMode.Value == SimulationMode.SideSwayPlusForward)
            {
                linAccel.right = (float)values[0];
                linAccel.forward = (float)values[1];
            }
            else if (settings.SimulationMode.Value == SimulationMode.SideSwayPlusUpward)
            {
                linAccel.right = (float)values[0];
                linAccel.upward = (float)values[1];
            }
        }
        else if (values.Length == 3)
        {
            linAccel.right = (float)values[0];
            linAccel.forward = (float)values[1];
            linAccel.upward = (float)values[2];
        }
    }
}
