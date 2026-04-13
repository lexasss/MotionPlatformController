namespace ValtraIMU.Models;

internal interface IAngular
{
    protected static double Deg2Rad(double degrees) => degrees * Math.PI / 180;
}
