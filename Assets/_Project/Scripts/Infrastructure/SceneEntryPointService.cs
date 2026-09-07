using System;
using ProjectSparks.Core;

namespace ProjectSparks.Infrastructure
{
    public sealed class SceneEntryPointService : ISceneEntryPointService
    {
        private string _pendingSpawnPointId = string.Empty;

        public void SetPending(string spawnPointId)
        {
            if (string.IsNullOrWhiteSpace(spawnPointId))
            {
                throw new ArgumentException(
                    "Идентификатор точки появления не может быть пустым.",
                    nameof(spawnPointId));
            }

            _pendingSpawnPointId = spawnPointId;
        }

        public bool TryConsume(out string spawnPointId)
        {
            spawnPointId = _pendingSpawnPointId;
            _pendingSpawnPointId = string.Empty;
            return !string.IsNullOrWhiteSpace(spawnPointId);
        }

        public void Clear()
        {
            _pendingSpawnPointId = string.Empty;
        }
    }
}
