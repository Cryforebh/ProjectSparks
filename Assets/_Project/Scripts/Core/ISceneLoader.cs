using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ProjectSparks.Core
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(string sceneName, Action<float> reportProgress, CancellationToken cancellationToken);
    }
}
