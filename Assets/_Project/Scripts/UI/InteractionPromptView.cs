using ProjectSparks.Player;
using TMPro;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [Tooltip("Корневой объект подсказки взаимодействия.")]
        [SerializeField] private GameObject _promptRoot;
        [Tooltip("Текст подсказки взаимодействия.")]
        [SerializeField] private TMP_Text _promptText;

        private IInteractionPromptSource _promptSource;

        [Inject]
        private void Construct(IInteractionPromptSource promptSource)
        {
            _promptSource = promptSource;
        }

        private void Start()
        {
            if (_promptRoot == null || _promptText == null)
            {
                Debug.LogError("В InteractionPromptView назначены не все ссылки.", this);
                enabled = false;
                return;
            }

            _promptSource.PromptChanged += HandlePromptChanged;
            HandlePromptChanged(_promptSource.CurrentPrompt);
        }

        private void OnDestroy()
        {
            if (_promptSource != null)
            {
                _promptSource.PromptChanged -= HandlePromptChanged;
            }
        }

        private void HandlePromptChanged(string prompt)
        {
            bool isVisible = !string.IsNullOrWhiteSpace(prompt);
            if (isVisible)
            {
                _promptText.text = prompt;
            }

            if (_promptRoot.activeSelf != isVisible)
            {
                _promptRoot.SetActive(isVisible);
            }
        }
    }
}
