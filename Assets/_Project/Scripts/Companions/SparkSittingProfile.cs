using System;
using UnityEngine;

namespace ProjectSparks.Companions
{
    [CreateAssetMenu(fileName = "SparkSittingProfile", menuName = "Project Sparks/Spark Sitting Profile")]
    public sealed class SparkSittingProfile : ScriptableObject
    {
        [Serializable]
        public sealed class Variant
        {
            [Tooltip("Полное имя состояния в базовом слое Animator, например Base Layer.SparkRig|Ilde_Sit.")]
            [SerializeField] private string _animatorState;
            [Min(0.1f), Tooltip("Минимальное время этого варианта сидения, в секундах. Не используется при завершении по анимации.")]
            [SerializeField] private float _minDuration = 3f;
            [Min(0.1f), Tooltip("Максимальное время этого варианта сидения, в секундах. Не используется при завершении по анимации.")]
            [SerializeField] private float _maxDuration = 6f;
            [Tooltip("Завершить сидение после первого проигрывания состояния Animator. Если включено, интервал времени не используется.")]
            [SerializeField] private bool _finishOnClipEnd;
            [Min(0f), Tooltip("Относительная частота выбора. Ноль исключает вариант из случайного выбора.")]
            [SerializeField] private float _selectionWeight = 1f;
            [Tooltip("Разрешить Искре поворачивать голову к игроку или другой Искре во время этого варианта сидения.")]
            [SerializeField] private bool _lookAtTargets;

            public string AnimatorState => _animatorState;
            public bool FinishOnClipEnd => _finishOnClipEnd;
            public float SelectionWeight => Mathf.Max(0f, _selectionWeight);
            public bool LookAtTargets => _lookAtTargets;

            public float ChooseDuration()
            {
                float minimum = Mathf.Max(0.1f, _minDuration);
                return UnityEngine.Random.Range(minimum, Mathf.Max(minimum, _maxDuration));
            }

            public bool IsSelectable => !string.IsNullOrWhiteSpace(_animatorState) && SelectionWeight > 0f;
        }

        [Tooltip("Доступные варианты сидения. Вес определяет частоту выбора среди вариантов с весом больше нуля.")]
        [SerializeField] private Variant[] _variants;

        public bool TryChoose(out Variant selected)
        {
            selected = null;
            if (_variants == null)
            {
                return false;
            }

            float totalWeight = 0f;
            foreach (Variant variant in _variants)
            {
                if (variant != null && variant.IsSelectable)
                {
                    totalWeight += variant.SelectionWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            float choice = UnityEngine.Random.value * totalWeight;
            foreach (Variant variant in _variants)
            {
                if (variant == null || !variant.IsSelectable)
                {
                    continue;
                }

                selected = variant;
                choice -= variant.SelectionWeight;
                if (choice <= 0f)
                {
                    return true;
                }
            }

            return selected != null;
        }
    }
}
