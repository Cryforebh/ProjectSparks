using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Missions;

namespace ProjectSparks.Core
{
    public interface IProgressService
    {
        bool IsLoaded { get; }

        UniTask LoadAsync(CancellationToken cancellationToken);
        UniTask ResetAsync(CancellationToken cancellationToken);
        UniTask CommitTripAsync(TripResult result, CancellationToken cancellationToken);
    }
}
