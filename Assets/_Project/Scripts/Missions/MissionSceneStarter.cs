using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using ProjectSparks.Levels;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionSceneStarter : MonoBehaviour
    {
        [Tooltip("Список складов и мест доставки этой сцены.")]
        [SerializeField] private MissionScenePoints _scenePoints;
        [Tooltip("Число доставок, название и награда для случайного режима.")]
        [SerializeField] private RandomMissionSettings _randomSettings;


        private IMissionService _missionService;
        private IProgressService _progressService;
        private ITripSessionService _tripSession;
        private LevelCatalog _levelCatalog;
        private MissionAssignment[] _assignments;
        private int _currentMissionIndex;
        private bool _isSubscribed;

        [Inject]
        public void Construct(
            IMissionService missionService,
            IProgressService progressService,
            ITripSessionService tripSession, LevelCatalog levelCatalog)
        {
            _missionService = missionService;
            _progressService = progressService;
            _tripSession = tripSession;
            _levelCatalog = levelCatalog;
        }

        private void Start()
        {
            if (!PrepareAssignments())
            {
                return;
            }

            if (!_tripSession.TryBegin(_levelCatalog.GetLocationId(gameObject.scene.name), _assignments.Length))
            {
                Debug.LogError("Не удалось начать поездку: сессия уже активна.", this);
                return;
            }

            StartMissionAsync().Forget();
        }

        private void OnDestroy()
        {
            if (_isSubscribed)
            {
                _missionService.StateChanged -= HandleMissionStateChanged;
            }
        }

        private bool PrepareAssignments()
        {
            if (_scenePoints == null || _scenePoints.gameObject.scene != gameObject.scene)
            {
                Debug.LogError("В MissionSceneStarter назначьте список точек этой сцены.", this);
                return false;
            }

            var random = new System.Random(Guid.NewGuid().GetHashCode());
            if (!RandomMissionGenerator.TryGenerate(_randomSettings, _scenePoints, random,
                out _assignments, out string generationError))
            {
                Debug.LogError(generationError, this);
                return false;
            }
            return true;
        }

        private void HandleMissionStateChanged()
        {
            if (_missionService.Phase != MissionPhase.Completed ||
                _currentMissionIndex >= _assignments.Length ||
                !ReferenceEquals(_missionService.ActiveMission, _assignments[_currentMissionIndex]))
            {
                return;
            }

            _currentMissionIndex++;
            if (_currentMissionIndex < _assignments.Length)
            {
                StartCurrentMission();
            }
        }

        private void StartCurrentMission()
        {
            if (!_missionService.TryStart(_assignments[_currentMissionIndex]))
            {
                Debug.LogError($"Не удалось начать миссию {_currentMissionIndex + 1}. Проверьте состояние поездки.", this);
            }
        }

        private async UniTask StartMissionAsync()
        {
            try
            {
                await _progressService.LoadAsync(this.GetCancellationTokenOnDestroy());
                _missionService.StateChanged += HandleMissionStateChanged;
                _isSubscribed = true;
                StartCurrentMission();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
