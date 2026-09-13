using System;
using System.IO;
using System.Threading;
using System.Text;
using Cysharp.Threading.Tasks;
using ProjectSparks.Companions;
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
                return CreateDefaultProgress();
            }

            return JsonUtility.FromJson<GameProgressData>(json) ?? CreateDefaultProgress();
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
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.RunOnThreadPool(
                () => WriteSaveFile(json),
                cancellationToken: CancellationToken.None);
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
            string temporaryPath = _saveFilePath + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(_saveFilePath))
            {
                File.Replace(temporaryPath, _saveFilePath, null);
            }
            else
            {
                File.Move(temporaryPath, _saveFilePath);
            }
        }

        private void DeleteSaveFile()
        {
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
            }
        }

        private static GameProgressData CreateDefaultProgress()
        {
            return new GameProgressData(0, string.Empty, 0);
        }
    }
}
