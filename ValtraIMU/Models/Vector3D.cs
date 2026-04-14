namespace ValtraIMU.Models;

internal record class Vector3D(
    double X,
    double Y,
    double Z
)
{
    public double[] ToArray() => [X, Y, Z];
    public static Vector3D FromArray(double[] arr) => new(arr[0], arr[1], arr[2]);
}
