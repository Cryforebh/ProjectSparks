using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ProjectSparks.Infrastructure
{
    public sealed class JsonFileSaveService : ISaveService
    {
        private const string SaveFileName = "progress.json";

        private readonly string _saveDirectoryPath;
        private readonly string _saveFilePath;

        public JsonFileSaveService(string saveDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(saveDirectoryPath))
            {
                throw new ArgumentException("Путь сохранения не может быть пустым.", nameof(saveDirectoryPath));
            }

            _saveDirectoryPath = saveDirectoryPath;
            _saveFilePath = Path.Combine(_saveDirectoryPath, SaveFileName);
        }

        public async UniTask<GameProgressData> LoadAsync(CancellationToken cancellationToken)
        {
            string json = await UniTask.RunOnThreadPool(
                ReadSaveFile,
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new GameProgressData(0);
            }

            return JsonUtility.FromJson<GameProgressData>(json) ?? new GameProgressData(0);
        }

        public async UniTask SaveAsync(
            GameProgressData data,
            CancellationToken cancellationToken)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string json = JsonUtility.ToJson(data, true);
            await UniTask.RunOnThreadPool(
                () => WriteSaveFile(json),
                cancellationToken: cancellationToken);
        }

        public async UniTask DeleteAsync(CancellationToken cancellationToken)
        {
            await UniTask.RunOnThreadPool(
                DeleteSaveFile,
                cancellationToken: cancellationToken);
        }

        private string ReadSaveFile()
        {
            return File.Exists(_saveFilePath)
                ? File.ReadAllText(_saveFilePath)
                : string.Empty;
        }

        private void WriteSaveFile(string json)
        {
            Directory.CreateDirectory(_saveDirectoryPath);
            File.WriteAllText(_saveFilePath, json);
        }

        private void DeleteSaveFile()
        {
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
            }
        }
    }
}
