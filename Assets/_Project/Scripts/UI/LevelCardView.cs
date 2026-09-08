using System;
using ProjectSparks.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSparks.UI
{
    public sealed class LevelCardView : MonoBehaviour
    {
        [Tooltip("Название и описание локации.")]
        [SerializeField] private TMP_Text _descriptionText;
        [Tooltip("Кнопка отправления в локацию.")]
        [SerializeField] private Button _launchButton;
        [Tooltip("Надпись на кнопке отправления.")]
        [SerializeField] private TMP_Text _buttonText;

        private LevelDefinition _level;
        private bool _isAvailable;
        private Action<LevelDefinition> _launch;

        public void Initialize(LevelDefinition level, bool isAvailable, Action<LevelDefinition> launch)
        {
            _level = level;
            _isAvailable = isAvailable;
            _launch = launch;
            _descriptionText.text = $"<b>{level.Title}</b>\n{level.Description}";
            _buttonText.text = isAvailable ? "Отправиться" : "Скоро";
            _launchButton.onClick.AddListener(Launch);
        }

        public void SetCanLaunch(bool canLaunch) => _launchButton.interactable = _isAvailable && canLaunch;

        private void Launch() => _launch?.Invoke(_level);

        private void OnDestroy() => _launchButton.onClick.RemoveListener(Launch);
    }
}
