using ProjectSparks.Companions;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerLookTarget : MonoBehaviour
    {
        [Tooltip("Точка, на которую смотрят Искры. Для другого вида камеры можно назначить, например, голову персонажа.")]
        [SerializeField] private Transform _target;

        private ISparkLookTargets _lookTargets;

        public Transform Target => _target;

        [Inject]
        private void Construct(ISparkLookTargets lookTargets)
        {
            _lookTargets = lookTargets;
            if (isActiveAndEnabled)
            {
                _lookTargets.RegisterPlayer(this);
            }
        }

        private void OnEnable()
        {
            _lookTargets?.RegisterPlayer(this);
        }

        private void OnDisable()
        {
            _lookTargets?.UnregisterPlayer(this);
        }
    }
}
