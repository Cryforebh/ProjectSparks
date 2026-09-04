namespace ProjectSparks.Driving
{
    public interface IVehicleInputSource
    {
        float Steer { get; }
        float Drive { get; }
        bool BrakeHeld { get; }
        bool BoostHeld { get; }

        bool ConsumeReset();
    }
}
