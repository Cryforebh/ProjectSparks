using ProjectSparks.Missions;
using TMPro;
using UnityEngine;
using Zenject;

namespace ProjectSparks.UI
{
    [DisallowMultipleComponent]
    public sealed class ResourceHudView : MonoBehaviour
    {
        [Tooltip("Текст фруктов, собранных за текущую поездку.")]
        [SerializeField] private TMP_Text _fruitCountText;
        [Tooltip("Текст капсул, найденных за текущую поездку.")]
        [SerializeField] private TMP_Text _capsuleCountText;

        private ITripSessionService _tripSession;

        [Inject]
        private void Construct(ITripSessionService tripSession)
        {
            _tripSession = tripSession;
        }

        private void Start()
        {
            if (_fruitCountText == null || _capsuleCountText == null)
            {
                Debug.LogError("В ResourceHudView назначены не все текстовые поля.", this);
                enabled = false;
                return;
            }

            _tripSession.Changed += RefreshAll;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_tripSession != null)
            {
                _tripSession.Changed -= RefreshAll;
            }
        }

        private void RefreshAll()
        {
            _fruitCountText.text = $"Фрукты: {_tripSession.FruitCount}";
            _capsuleCountText.text = $"Капсулы: {_tripSession.CapsuleCount}";
        }
    }
}
