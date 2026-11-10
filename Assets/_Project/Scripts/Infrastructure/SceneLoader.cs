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
        private const float ReadyToActivate = 0.9f;

        public async UniTask LoadAsync(
            string sceneName,
            Action<float> reportProgress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException(
                    "Имя загружаемой сцены не может быть пустым.",
                    nameof(sceneName));
            }

            cancellationToken.ThrowIfCancellationRequested();
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single);

            if (loadOperation == null)
            {
                throw new InvalidOperationException(
                    $"Не удалось начать загрузку сцены {sceneName}.");
            }

            loadOperation.allowSceneActivation = false;
            try
            {
                while (loadOperation.progress < ReadyToActivate)
                {
                    reportProgress(loadOperation.progress);
                    await UniTask.NextFrame(cancellationToken: cancellationToken);
                }

                reportProgress(ReadyToActivate);
                // Показываем прогресс до активации сцены на главном потоке.
                await UniTask.NextFrame(cancellationToken: cancellationToken);
            }
            finally
            {
                loadOperation.allowSceneActivation = true;
            }

            await loadOperation.ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
