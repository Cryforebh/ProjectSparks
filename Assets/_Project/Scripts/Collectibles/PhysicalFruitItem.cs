using ProjectSparks.Player;
using UnityEngine;

namespace ProjectSparks.Collectibles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PhysicalFruitItem : MonoBehaviour, IFruitItem, IInteractable
    {
        [Header("Фрукт")]
        [Tooltip("Постоянный уникальный ID фрукта. Не менять после появления сохранений с этим фруктом.")]
        [SerializeField] private string _fruitId;
        [Tooltip("Название фрукта в подсказке взаимодействия.")]
        [SerializeField] private string _displayName = "Фрукт";

        [Header("Точки обхвата")]
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private Transform _rightHandTarget;

        [Header("Физика")]
        [Tooltip("Твердое тело физического фрукта.")]
        [SerializeField] private Rigidbody _rigidbody;
        [Tooltip("Коллайдеры, которые включаются только у лежащего фрукта.")]
        [SerializeField] private Collider[] _colliders;

        private IFruitWorldService _fruitWorldService;
        private bool _isHeld;
        private bool _isAvailableInWorld;

        public string FruitId => _fruitId;
        public Transform LeftHandTarget => _leftHandTarget;
        public Transform RightHandTarget => _rightHandTarget;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
            ? "Фрукт"
            : _displayName;
        public HeldItemType ItemType => HeldItemType.Fruit;
        public Transform ItemTransform => transform;
        public string InteractionPrompt => $"Взять {DisplayName.ToLowerInvariant()}";
        public bool CanInteract => _isAvailableInWorld && !_isHeld;

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider>(true);
            }

            SetPhysicsEnabled(false);
        }

        public void Initialize(IFruitWorldService fruitWorldService)
        {
            _fruitWorldService = fruitWorldService;
        }

        public void Interact()
        {
            if (CanInteract)
            {
                _fruitWorldService?.TryPickUp(this);
            }
        }

        public void SetHeld(bool isHeld)
        {
            _isHeld = isHeld;
            _isAvailableInWorld = false;
            SetPhysicsEnabled(false);
        }

        public void PrepareForHand(Transform poolRoot)
        {
            gameObject.SetActive(true);
            transform.SetParent(poolRoot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _isHeld = false;
            _isAvailableInWorld = false;
            SetPhysicsEnabled(false);
        }

        public void PlaceInWorld(
            Transform worldRoot,
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity)
        {
            gameObject.SetActive(true);
            transform.SetParent(worldRoot, true);
            transform.SetPositionAndRotation(position, rotation);
            _isHeld = false;
            _isAvailableInWorld = true;
            SetPhysicsEnabled(true);
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Random.insideUnitSphere * 2f;
        }

        public void ReturnToPool(Transform poolRoot)
        {
            _isHeld = false;
            _isAvailableInWorld = false;
            SetPhysicsEnabled(false);
            transform.SetParent(poolRoot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            gameObject.SetActive(false);
        }

        private void SetPhysicsEnabled(bool isEnabled)
        {
            if (_rigidbody != null)
            {
                if (isEnabled)
                {
                    _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    _rigidbody.detectCollisions = true;
                    _rigidbody.isKinematic = false;
                    _rigidbody.useGravity = true;
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    if (!_rigidbody.isKinematic)
                    {
                        _rigidbody.linearVelocity = Vector3.zero;
                        _rigidbody.angularVelocity = Vector3.zero;
                    }

                    _rigidbody.interpolation = RigidbodyInterpolation.None;
                    _rigidbody.detectCollisions = false;
                    _rigidbody.useGravity = false;
                    _rigidbody.isKinematic = true;
                }
            }

            if (_colliders == null)
            {
                return;
            }

            foreach (Collider fruitCollider in _colliders)
            {
                if (fruitCollider != null)
                {
                    fruitCollider.enabled = isEnabled;
                }
            }
        }

    }
}
