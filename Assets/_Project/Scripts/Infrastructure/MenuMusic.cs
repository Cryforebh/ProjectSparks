using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace ProjectSparks.Infrastructure
{
    public sealed class MenuMusic : IInitializable, IDisposable
    {
        private readonly AudioSource _source;

        public MenuMusic(AudioSource source)
        {
            _source = source;
        }

        public void Initialize()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            ApplyScene(SceneManager.GetActiveScene());
        }

        public void Dispose() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        private void OnActiveSceneChanged(Scene previous, Scene current) => ApplyScene(current);

        private void ApplyScene(Scene scene)
        {
            if (scene.name == "MainMenu" || scene.name == "Shelter")
            {
                if (!_source.isPlaying)
                    _source.Play();
            }
            else
            {
                _source.Stop();
            }
        }
    }
}
