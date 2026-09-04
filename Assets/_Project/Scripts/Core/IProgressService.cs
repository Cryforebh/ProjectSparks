using System.Threading;
using Cysharp.Threading.Tasks;

namespace ProjectSparks.Core
{
    public interface IProgressService
    {
        bool IsLoaded { get; }

        UniTask LoadAsync(CancellationToken cancellationToken);
        UniTask ResetAsync(CancellationToken cancellationToken);
    }
}
