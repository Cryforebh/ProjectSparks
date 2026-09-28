using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Missions;

namespace ProjectSparks.Core
{
    public interface IProgressService
    {
        event Action ResetCompleted;

        bool IsLoaded { get; }

        UniTask LoadAsync(CancellationToken cancellationToken);
        UniTask ResetAsync(CancellationToken cancellationToken);
        UniTask CommitTripAsync(TripResult result, CancellationToken cancellationToken);
        bool TryInitializeStarterFruits();
    }
}
