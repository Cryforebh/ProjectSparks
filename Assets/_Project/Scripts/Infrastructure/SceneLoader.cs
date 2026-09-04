using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSparks.Infrastructure
{
    public sealed class SceneLoader : ISceneLoader
    {
        public async UniTask LoadAsync(
            string sceneName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException(
                    "Имя загружаемой сцены не может быть пустым.",
                    nameof(sceneName));
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single);

            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"Не удалось начать загрузку сцены {sceneName}.");
            }

            await loadOperation.ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
