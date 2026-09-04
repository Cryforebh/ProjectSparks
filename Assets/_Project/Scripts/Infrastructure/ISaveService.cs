using System.Threading;
using Cysharp.Threading.Tasks;

namespace ProjectSparks.Infrastructure
{
    public interface ISaveService
    {
        UniTask<GameProgressData> LoadAsync(CancellationToken cancellationToken);
        UniTask SaveAsync(GameProgressData data, CancellationToken cancellationToken);
        UniTask DeleteAsync(CancellationToken cancellationToken);
    }
}
