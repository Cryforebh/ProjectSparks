using System;
using System.Collections.Generic;
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
        [Tooltip("Миссии уровня в порядке выполнения. Каждая должна иметь уникальный идентификатор.")]
        [SerializeField] private MissionDefinition[] _missions = Array.Empty<MissionDefinition>();

        private IMissionService _missionService;
        private IProgressService _progressService;
        private ITripSessionService _tripSession;
        private LevelCatalog _levelCatalog;
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
            if (!ValidateMissions())
            {
                return;
            }

            if (!_tripSession.TryBegin(_levelCatalog.GetLocationId(gameObject.scene.name), _missions.Length))
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

        private bool ValidateMissions()
        {
            if (_missions == null || _missions.Length == 0)
            {
                Debug.LogError("В MissionSceneStarter список миссий пуст.", this);
                return false;
            }

            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _missions.Length; index++)
            {
                MissionDefinition mission = _missions[index];
                if (mission == null || string.IsNullOrWhiteSpace(mission.Id) ||
                    !identifiers.Add(mission.Id) || mission.Reward < 0)
                {
                    Debug.LogError($"Миссия {index + 1}: назначьте задание с уникальным непустым идентификатором и неотрицательной наградой.", this);
                    return false;
                }
            }

            return true;
        }

        private void HandleMissionStateChanged()
        {
            if (_missionService.Phase != MissionPhase.Completed ||
                _currentMissionIndex >= _missions.Length ||
                !ReferenceEquals(_missionService.ActiveMission, _missions[_currentMissionIndex]))
            {
                return;
            }

            _currentMissionIndex++;
            if (_currentMissionIndex < _missions.Length)
            {
                StartCurrentMission();
            }
        }

        private void StartCurrentMission()
        {
            if (!_missionService.TryStart(_missions[_currentMissionIndex]))
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
