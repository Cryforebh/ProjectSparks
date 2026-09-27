using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class SparkFruitAttachment : MonoBehaviour
    {
        [Tooltip("Точка крепления фрукта на визуальной модели Искры.")]
        [SerializeField] private Transform _anchor;

        [Tooltip("Метка на GrowthBody, за которой следует точка крепления без наследования масштаба.")]
        [SerializeField] private Transform _growthGuide;

        public Transform Anchor => _anchor;

        private void Awake()
        {
            if (_anchor == null || _growthGuide == null)
            {
                Debug.LogError("Не настроены точки крепления фрукта к Искре.", this);
                enabled = false;
                return;
            }

            // Сохраняем выставленное вручную положение якоря для взрослой Искры.
            _growthGuide.SetPositionAndRotation(_anchor.position, _anchor.rotation);
        }

        private void LateUpdate()
        {
            _anchor.SetPositionAndRotation(_growthGuide.position, _growthGuide.rotation);
        }
    }
}
