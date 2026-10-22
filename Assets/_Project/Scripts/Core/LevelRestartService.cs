using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;

namespace ProjectSparks.Core
{
    public sealed class LevelRestartService : IInitializable, IDisposable
    {
        private readonly ISceneTransitionService _transition;
        private InputAction _restartAction;
        private string _sceneName;
        private bool _isRestarting;

        public LevelRestartService(ISceneTransitionService transition)
        {
            _transition = transition;
        }

        public void Initialize()
        {
            _sceneName = SceneManager.GetActiveScene().name;
            _restartAction = new InputAction("RestartLevel", InputActionType.Button,
                "<Keyboard>/backquote", interactions: "Hold(duration=1.5)");
            _restartAction.performed += HandleRestart;
            _restartAction.Enable();
        }

        public void Dispose()
        {
            if (_restartAction == null)
                return;

            _restartAction.performed -= HandleRestart;
            _restartAction.Dispose();
            _restartAction = null;
        }

        private void HandleRestart(InputAction.CallbackContext context)
        {
            if (!_isRestarting && !_transition.IsTransitioning)
                RestartAsync().Forget();
        }

        private async UniTask RestartAsync()
        {
            _isRestarting = true;
            try
            {
                // Выгрузка старой сцены не должна отменять ее перезапуск.
                await _transition.TransitionAsync(_sceneName, string.Empty, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isRestarting = false;
            }
        }
    }
}
