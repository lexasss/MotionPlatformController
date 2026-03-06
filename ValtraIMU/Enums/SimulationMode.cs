namespace ValtraIMU;

internal enum SimulationMode
{
    /// <summary>
    /// Simple sine wave simulation.
    /// </summary>
    SineWave,
    /// <summary>
    /// A pusle imitating moving forward, moving forward for a short time, then stopping.
    /// </summary>
    MoveForward,
    /// <summary>
    /// Sine wave imitating swaying up/down.
    /// </summary>
    SwayForward,
    /// <summary>
    /// Sine wave imitating swaying left/right.
    /// </summary>
    SwayAside
}
