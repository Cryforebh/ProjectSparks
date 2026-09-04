using System;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Core
{
    public sealed class PauseService : IPauseService, ITickable, IDisposable
    {
        private readonly IPauseInputSource _input;
        private float _resumeTimeScale = 1f;

        public event Action<bool> PauseChanged;

        public bool IsPaused { get; private set; }

        public PauseService(IPauseInputSource input)
        {
            _input = input;
        }

        public void Tick()
        {
            if (_input.ConsumePause())
            {
                TogglePause();
            }
        }

        public void SetPaused(bool isPaused)
        {
            if (IsPaused == isPaused)
            {
                return;
            }

            IsPaused = isPaused;
            if (isPaused)
            {
                _resumeTimeScale = Mathf.Approximately(Time.timeScale, 0f)
                    ? 1f
                    : Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _resumeTimeScale;
            }

            PauseChanged?.Invoke(IsPaused);
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        public void Dispose()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = _resumeTimeScale;
        }
    }
}
