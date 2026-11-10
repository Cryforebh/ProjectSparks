using System;
using ProjectSparks.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsMenuView : MonoBehaviour
    {
        [Serializable]
        private sealed class Choice
        {
            [SerializeField] private Button _previous;
            [SerializeField] private Button _next;
            [SerializeField] private TMP_Text _value;

            public void Bind(Action<int> change)
            {
                _previous.onClick.AddListener(() => change(-1));
                _next.onClick.AddListener(() => change(1));
            }

            public void Show(string text, int index, int count, bool available = true)
            {
                _value.text = text;
                _previous.interactable = available && index > 0;
                _next.interactable = available && index < count - 1;
            }
        }

        private static readonly int[] FrameRates = { 60, 90, 120 };
        private static readonly string[] AntialiasingLabels = { "Выкл.", "SMAA", "MSAA ×2", "MSAA ×4", "MSAA ×8" };

        [SerializeField] private Button _open;
        [SerializeField] private Button _close;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _graphicsTab;
        [SerializeField] private Button _audioTab;
        [SerializeField] private RectTransform _tabIndicator;
        [SerializeField] private GameObject _graphicsPage;
        [SerializeField] private GameObject _audioPage;

        [SerializeField] private Choice _resolution;
        [SerializeField] private Choice _frameRate;
        [SerializeField] private Choice _antialiasing;
        [SerializeField] private Button _vSync;
        [SerializeField] private TMP_Text _vSyncValue;
        [SerializeField] private GameObject _vSyncHint;
        [SerializeField] private Slider _masterVolume;
        [SerializeField] private TMP_Text _volumeValue;

        private GameSettings _settings;

        [Inject]
        private void Construct(GameSettings settings) => _settings = settings;

        private void Start()
        {
            _open.onClick.AddListener(Open);
            _close.onClick.AddListener(Close);
            _graphicsTab.onClick.AddListener(() => ShowPage(true));
            _audioTab.onClick.AddListener(() => ShowPage(false));
            _resolution.Bind(ChangeResolution);
            _frameRate.Bind(ChangeFrameRate);
            _antialiasing.Bind(ChangeAntialiasing);
            _vSync.onClick.AddListener(ToggleVSync);
            _masterVolume.onValueChanged.AddListener(ChangeVolume);
        }

        private void Open()
        {
            RefreshGraphics();
            _masterVolume.SetValueWithoutNotify(_settings.MasterVolume);
            ShowVolume(_settings.MasterVolume);
            ShowPage(true);
            _panel.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_close.gameObject);
        }

        private void Close()
        {
            _settings.Save();
            _panel.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(_open.gameObject);
        }

        private void ShowPage(bool graphics)
        {
            _graphicsPage.SetActive(graphics);
            _audioPage.SetActive(!graphics);
            _graphicsTab.interactable = !graphics;
            _audioTab.interactable = graphics;
            RectTransform activeTab = (RectTransform)(graphics ? _graphicsTab : _audioTab).transform;
            _tabIndicator.anchoredPosition = new Vector2(activeTab.anchoredPosition.x, _tabIndicator.anchoredPosition.y);
        }

        private int ResolutionIndex()
        {
            for (int i = 0; i < _settings.Resolutions.Count; i++)
            {
                if (_settings.Resolutions[i] == _settings.Resolution)
                    return i;
            }

            return 0;
        }

        private void RefreshGraphics()
        {
            Vector2Int size = _settings.Resolution;
            _resolution.Show($"{size.x} × {size.y}", ResolutionIndex(), _settings.Resolutions.Count);
            _frameRate.Show($"{_settings.FrameRate} FPS", Array.IndexOf(FrameRates, _settings.FrameRate),
                FrameRates.Length, !_settings.VSync);
            int aa = (int)_settings.Antialiasing;
            _antialiasing.Show(AntialiasingLabels[aa], aa, AntialiasingLabels.Length);
            _vSyncValue.text = _settings.VSync ? "Вкл." : "Выкл.";
            _vSyncHint.SetActive(_settings.VSync);
        }

        private void ChangeResolution(int step)
        {
            int index = Mathf.Clamp(ResolutionIndex() + step, 0, _settings.Resolutions.Count - 1);
            _settings.SetResolution(_settings.Resolutions[index]);
            RefreshGraphics();
        }

        private void ChangeFrameRate(int step)
        {
            int index = Mathf.Clamp(Array.IndexOf(FrameRates, _settings.FrameRate) + step, 0, FrameRates.Length - 1);
            _settings.SetFrameRate(FrameRates[index]);
            RefreshGraphics();
        }

        private void ChangeAntialiasing(int step)
        {
            int index = Mathf.Clamp((int)_settings.Antialiasing + step, 0, AntialiasingLabels.Length - 1);
            _settings.SetAntialiasing((GameAntialiasing)index);
            RefreshGraphics();
        }

        private void ToggleVSync()
        {
            _settings.SetVSync(!_settings.VSync);
            RefreshGraphics();
        }

        private void ChangeVolume(float volume)
        {
            _settings.SetMasterVolume(volume);
            ShowVolume(volume);
        }

        private void ShowVolume(float volume)
        {
            _volumeValue.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
}
