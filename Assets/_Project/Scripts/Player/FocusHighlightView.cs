using UnityEngine;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class FocusHighlightView : MonoBehaviour, IFocusable
    {
        [Tooltip("Объекты визуальной подсветки, которые включаются при наведении.")]
        [SerializeField] private GameObject[] _highlightVisuals;

        private void Awake()
        {
            SetFocused(false);
        }

        private void OnDisable()
        {
            SetFocused(false);
        }

        public void SetFocused(bool isFocused)
        {
            if (_highlightVisuals == null)
            {
                return;
            }

            foreach (GameObject highlightVisual in _highlightVisuals)
            {
                if (highlightVisual != null && highlightVisual != gameObject)
                {
                    highlightVisual.SetActive(isFocused);
                }
            }
        }
    }
}
