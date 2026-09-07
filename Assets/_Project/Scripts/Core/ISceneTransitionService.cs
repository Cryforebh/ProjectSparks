using System.Threading;
using Cysharp.Threading.Tasks;

namespace ProjectSparks.Core
{
    public interface ISceneTransitionService
    {
        bool IsTransitioning { get; }

        UniTask TransitionAsync(
            string sceneName,
            string spawnPointId,
            CancellationToken cancellationToken);
    }
}
