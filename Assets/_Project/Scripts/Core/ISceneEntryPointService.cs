namespace ProjectSparks.Core
{
    public interface ISceneEntryPointService
    {
        void SetPending(string spawnPointId);
        bool TryConsume(out string spawnPointId);
        void Clear();
    }
}
