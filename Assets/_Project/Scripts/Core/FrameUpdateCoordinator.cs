using UnityEngine;

namespace ProjectSparks.Core
{
    [DisallowMultipleComponent]
    public sealed class FrameUpdateCoordinator : MonoBehaviour
    {
        [Tooltip("Компоненты с IFrameUpdate в порядке обновления. Отключенные компоненты пропускаются.")]
        [SerializeField] private MonoBehaviour[] _components = new MonoBehaviour[0];

        private IFrameUpdate[] _updates;

        private void Awake()
        {
            _updates = new IFrameUpdate[_components.Length];

            for (int i = 0; i < _components.Length; i++)
            {
                if (!(_components[i] is IFrameUpdate frameUpdate))
                {
                    Debug.LogError("В FrameUpdateCoordinator назначьте компоненты, реализующие IFrameUpdate, без пустых элементов.", this);
                    enabled = false;
                    return;
                }

                _updates[i] = frameUpdate;
            }
        }

        private void Update()
        {
            for (int i = 0; i < _updates.Length; i++)
            {
                MonoBehaviour component = _components[i];
                if (component != null && component.isActiveAndEnabled)
                {
                    _updates[i].UpdateFrame();
                }
            }
        }
    }
}
