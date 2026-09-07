using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class SceneSpawnController : MonoBehaviour
    {
        [Tooltip("Корневой объект, который перемещается в точку появления.")]
        [SerializeField] private Transform _targetRoot;
        [Tooltip("Необязательный контроллер персонажа, отключаемый при перемещении.")]
        [SerializeField] private CharacterController _characterController;
        [Tooltip("Доступные точки появления в этой сцене.")]
        [SerializeField] private SceneSpawnPoint[] _spawnPoints;

        private ISceneEntryPointService _entryPointService;

        [Inject]
        private void Construct(ISceneEntryPointService entryPointService)
        {
            _entryPointService = entryPointService;
        }

        private void Start()
        {
            if (_targetRoot == null)
            {
                Debug.LogError("В SceneSpawnController не назначен перемещаемый объект.", this);
                enabled = false;
                return;
            }

            if (!_entryPointService.TryConsume(out string spawnPointId))
            {
                return;
            }

            SceneSpawnPoint spawnPoint = FindSpawnPoint(spawnPointId);
            if (spawnPoint == null)
            {
                Debug.LogWarning(
                    $"В сцене не найдена точка появления {spawnPointId}.",
                    this);
                return;
            }

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            _targetRoot.SetPositionAndRotation(
                spawnPoint.transform.position,
                spawnPoint.transform.rotation);

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }
        }

        private SceneSpawnPoint FindSpawnPoint(string spawnPointId)
        {
            if (_spawnPoints == null)
            {
                return null;
            }

            foreach (SceneSpawnPoint spawnPoint in _spawnPoints)
            {
                if (spawnPoint != null && spawnPoint.Id == spawnPointId)
                {
                    return spawnPoint;
                }
            }

            return null;
        }
    }
}
