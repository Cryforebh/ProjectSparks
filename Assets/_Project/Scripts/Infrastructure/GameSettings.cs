using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Zenject;

namespace ProjectSparks.Infrastructure
{
    public enum GameAntialiasing
    {
        Off,
        Smaa,
        Msaa2x,
        Msaa4x,
        Msaa8x
    }

    public sealed class GameSettings : IInitializable, IDisposable
    {
        private const string FrameRateKey = "Settings.FrameRate";
        private const string VSyncKey = "Settings.VSync";
        private const string AntialiasingKey = "Settings.Antialiasing";
        private const string VolumeKey = "Settings.MasterVolume";
        private const string WidthKey = "Settings.ScreenWidth";
        private const string HeightKey = "Settings.ScreenHeight";

        private RenderPipelineAsset _originalQualityPipeline;
        private UniversalRenderPipelineAsset _runtimePipeline;
        private int _originalVSyncCount;
        private int _originalTargetFrameRate;
        private float _originalVolume;

        public IReadOnlyList<Vector2Int> Resolutions { get; private set; }
        public Vector2Int Resolution { get; private set; }
        public int FrameRate { get; private set; }
        public bool VSync { get; private set; }
        public GameAntialiasing Antialiasing { get; private set; }
        public float MasterVolume { get; private set; }

        public void Initialize()
        {
            FrameRate = PlayerPrefs.GetInt(FrameRateKey, 60);
            if (FrameRate != 60 && FrameRate != 90 && FrameRate != 120)
                FrameRate = 60;
            VSync = PlayerPrefs.GetInt(VSyncKey, 0) == 1;
            Antialiasing = (GameAntialiasing)PlayerPrefs.GetInt(AntialiasingKey, (int)GameAntialiasing.Msaa8x);
            if (!Enum.IsDefined(typeof(GameAntialiasing), Antialiasing))
                Antialiasing = GameAntialiasing.Msaa8x;
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));

            Vector2Int currentResolution = new Vector2Int(Screen.width, Screen.height);
            Resolutions = Screen.resolutions
                .Select(mode => new Vector2Int(mode.width, mode.height))
                .Append(currentResolution)
                .Distinct()
                .OrderBy(size => size.x)
                .ThenBy(size => size.y)
                .ToArray();
            Resolution = new Vector2Int(PlayerPrefs.GetInt(WidthKey, currentResolution.x),
                PlayerPrefs.GetInt(HeightKey, currentResolution.y));
            if (!Resolutions.Contains(Resolution))
                Resolution = currentResolution;
            if (Resolution != currentResolution)
                ApplyResolution();

            _originalVSyncCount = QualitySettings.vSyncCount;
            _originalTargetFrameRate = Application.targetFrameRate;
            _originalVolume = AudioListener.volume;
            ApplyFrameRate();
            AudioListener.volume = MasterVolume;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                _originalQualityPipeline = QualitySettings.renderPipeline;
                _runtimePipeline = UnityEngine.Object.Instantiate(pipeline);
                _runtimePipeline.name = pipeline.name + " (Game Settings)";
                _runtimePipeline.hideFlags = HideFlags.DontSave;
                ApplyMsaa();
                QualitySettings.renderPipeline = _runtimePipeline;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyCameraAntialiasing(SceneManager.GetActiveScene());
        }

        public void SetResolution(Vector2Int resolution)
        {
            if (resolution == Resolution || !Resolutions.Contains(resolution))
                return;

            Resolution = resolution;
            ApplyResolution();
            PlayerPrefs.SetInt(WidthKey, resolution.x);
            PlayerPrefs.SetInt(HeightKey, resolution.y);
        }

        public void SetFrameRate(int frameRate)
        {
            FrameRate = frameRate;
            ApplyFrameRate();
            PlayerPrefs.SetInt(FrameRateKey, frameRate);
        }

        public void SetVSync(bool enabled)
        {
            VSync = enabled;
            ApplyFrameRate();
            PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
        }

        public void SetAntialiasing(GameAntialiasing antialiasing)
        {
            Antialiasing = antialiasing;
            ApplyMsaa();
            ApplyCameraAntialiasing(SceneManager.GetActiveScene());
            PlayerPrefs.SetInt(AntialiasingKey, (int)antialiasing);
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        }

        public void Save() => PlayerPrefs.Save();

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            QualitySettings.vSyncCount = _originalVSyncCount;
            Application.targetFrameRate = _originalTargetFrameRate;
            AudioListener.volume = _originalVolume;
            if (_runtimePipeline == null)
                return;

            if (QualitySettings.renderPipeline == _runtimePipeline)
                QualitySettings.renderPipeline = _originalQualityPipeline;
            UnityEngine.Object.Destroy(_runtimePipeline);
        }

        private void ApplyResolution()
        {
            Screen.SetResolution(Resolution.x, Resolution.y, Screen.fullScreenMode);
        }

        private void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameRate;
        }

        private void ApplyMsaa()
        {
            if (_runtimePipeline == null)
                return;

            _runtimePipeline.msaaSampleCount = Antialiasing switch
            {
                GameAntialiasing.Msaa2x => 2,
                GameAntialiasing.Msaa4x => 4,
                GameAntialiasing.Msaa8x => 8,
                _ => 1
            };
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyCameraAntialiasing(scene);

        private void ApplyCameraAntialiasing(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            AntialiasingMode mode = Antialiasing == GameAntialiasing.Smaa
                ? AntialiasingMode.SubpixelMorphologicalAntiAliasing
                : AntialiasingMode.None;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (UniversalAdditionalCameraData data in root.GetComponentsInChildren<UniversalAdditionalCameraData>(true))
                {
                    Camera camera = data.GetComponent<Camera>();
                    if (camera.cameraType == CameraType.Game && camera.CompareTag("MainCamera"))
                        data.antialiasing = mode;
                }
            }
        }
    }
}
