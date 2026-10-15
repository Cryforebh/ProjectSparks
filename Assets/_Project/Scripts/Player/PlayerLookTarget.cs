using UnityEngine;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerLookTarget : MonoBehaviour
    {
        [Tooltip("Точка, на которую смотрят Искры. Для другого вида камеры можно назначить, например, голову персонажа.")]
        [SerializeField] private Transform _target;

        private static PlayerLookTarget _active;

        public static Transform Current => _active != null ? _active._target : null;

        private void OnEnable()
        {
            _active = this;
        }

        private void OnDisable()
        {
            if (_active == this)
            {
                _active = null;
            }
        }
    }
}
