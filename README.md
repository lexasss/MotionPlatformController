# MotionPlatform controller

A set of tools to stream data into MotionPlatform device using its APIs, like ForceSeatMI.

## ForceSeatMI project

ForceSeatMI source file were used to build a .NET library taht is linked to other tools

## ValtraIMU project

A console app that reads a log file with IMU+GNSS data provided by Valtra, and streams data into the MotionPlatform device.

The following scheme shows how IMU+GNSS data field are mapped onto MotionPlatform axes:

- `AccBdy` to `bodyLinearAcceleration`
	- X => right (Sway)
	- Y => forward (Surge)
	- Z => upward (Heave)
- `AngRate` to `bodyAngularVelocity`
	- Z => yaw
	- X => pitch
	- Y => roll
- `Pitch` to `bodyPitch`
- `Roll` to `bodyRoll`

If not log file provided, uses sine functions to generate angular velocity and linear acceleration data.
