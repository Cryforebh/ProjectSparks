using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionSceneStarter : MonoBehaviour
    {
        [Tooltip("Миссия, которая запускается при старте сцены.")]
        [SerializeField] private MissionDefinition _mission;

        private IMissionService _missionService;
        private IProgressService _progressService;

        [Inject]
        public void Construct(
            IMissionService missionService,
            IProgressService progressService)
        {
            _missionService = missionService;
            _progressService = progressService;
        }

        private void Start()
        {
            if (_mission == null)
            {
                Debug.LogError("В MissionSceneStarter не назначена миссия.", this);
                return;
            }

            StartMissionAsync().Forget();
        }

        private async UniTask StartMissionAsync()
        {
            try
            {
                await _progressService.LoadAsync(this.GetCancellationTokenOnDestroy());
                _missionService.TryStart(_mission);
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
