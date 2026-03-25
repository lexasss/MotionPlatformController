using System.ComponentModel;

namespace ValtraIMU;

[TypeConverter(typeof(FriendlyEnumConverter))]
internal enum SimulationMode
{
    /// <summary>
    /// Simple sine wave simulation.
    /// </summary>
    SineWaveAccel,
    /// <summary>
    /// A pusle imitating moving forward, moving forward for a short time, then stopping.
    /// </summary>
    MovePulse,
    /// <summary>
    /// Sine wave imitating swaying on a defined axis.
    /// </summary>
    Sway,
    /// <summary>
    /// Sine motion on two axes, with Pi/2 phase difference.
    /// </summary>
    CircluarSway,
    /// <summary>
    /// Sine acceleration on a defined axis.
    /// </summary>
    SineAcceleration,
    /// <summary>
    /// Sine acceleration on two axes (horizontal), with Pi/2 phase difference.
    /// </summary>
    CircluarSineAccelerationHorizontal,
    /// <summary>
    /// Sine acceleration on two axes (vertical), with Pi/2 phase difference.
    /// </summary>
    CircluarSineAccelerationVertical,
}
