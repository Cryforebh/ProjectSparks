namespace ProjectSparks.Driving
{
    public interface IVehicleTelemetry
    {
        UnityEngine.Vector3 Position { get; }
        float SpeedKph { get; }
        float Boost01 { get; }
    }
}
