using ProjectSparks.Core;
using ProjectSparks.Driving;
using ProjectSparks.UI;
using UnityEngine;
using Zenject;

namespace ProjectSparks.Missions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TripReturnZone : MonoBehaviour
    {
        [Header("Возвращение в убежище")]
        [Tooltip("Существующий TripResultsView из Canvas. Открывает те же окна, что и кнопка в меню.")]
        [SerializeField] private TripResultsView _tripResultsView;
        [Tooltip("Точка за пределами зоны, куда ставим машину после выбора «Остаться».")]
        [SerializeField] private Transform _cancelReturnPoint;
        [Tooltip("Слой машины, способной активировать возвращение.")]
        [SerializeField] private LayerMask _vehicleMask;

        private IPauseService _pauseService;
        private Rigidbody _requestingBody;
        private bool _awaitingDecision;

        [Inject]
        private void Construct(IPauseService pauseService)
        {
            _pauseService = pauseService;
        }

        private void OnEnable()
        {
            if (_tripResultsView != null)
                _tripResultsView.ReturnCancelled += HandleReturnCancelled;
        }

        private void OnDisable()
        {
            if (_tripResultsView != null)
                _tripResultsView.ReturnCancelled -= HandleReturnCancelled;
            _awaitingDecision = false;
            _requestingBody = null;
        }

        private void Start()
        {
            BoxCollider zoneCollider = GetComponent<BoxCollider>();
            if (!zoneCollider.isTrigger || _tripResultsView == null || _cancelReturnPoint == null || _pauseService == null)
            {
                Debug.LogError("TripReturnZone: проверьте Is Trigger, TripResultsView, Cancel Return Point и Zenject.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_awaitingDecision || _pauseService == null || _pauseService.IsPaused || _tripResultsView == null)
                return;

            Rigidbody body = other.attachedRigidbody;
            if (body == null || (body.GetComponent<ArcadeCarController>() == null) ||
                (_vehicleMask.value & (1 << body.gameObject.layer)) == 0)
                return;

            _requestingBody = body;
            _awaitingDecision = true;

            // Этот метод открывает окно итогов после выполнения доставок,
            // либо предупреждение о потере наград до их выполнения.
            _tripResultsView.RequestReturn();

            // Если UI отклонил запрос (например, ещё не инициализирован),
            // не оставляем зону заблокированной.
            if (!_pauseService.IsPaused)
            {
                _awaitingDecision = false;
                _requestingBody = null;
            }
        }

        private void HandleReturnCancelled()
        {
            if (!_awaitingDecision)
                return;

            _awaitingDecision = false;
            Rigidbody body = _requestingBody;
            _requestingBody = null;
            if (body == null || _cancelReturnPoint == null)
                return;

            body.position = _cancelReturnPoint.position;
            body.rotation = _cancelReturnPoint.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            Physics.SyncTransforms();
        }

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            _vehicleMask = LayerMask.GetMask("Vehicle");
        }
    }
}
