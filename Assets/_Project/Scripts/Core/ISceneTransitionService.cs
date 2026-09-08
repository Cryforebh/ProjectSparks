using System.Threading;
using Cysharp.Threading.Tasks;

namespace ProjectSparks.Core
{
    public interface ISceneTransitionService
    {
        bool IsTransitioning { get; }

        // Пустая точка появления сохраняет стартовую позицию, заданную в сцене.
        UniTask TransitionAsync(
            string sceneName,
            string spawnPointId,
            CancellationToken cancellationToken);
    }
}
