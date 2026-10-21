using UnityEngine;
using UnityEngine.AI;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SparkInteractable), typeof(NavMeshAgent))]
    public sealed class SparkWanderNavigation : MonoBehaviour
    {
        [Tooltip("Варианты сидения, их длительность и разрешение поворачивать голову.")]
        [SerializeField] private SparkSittingProfile _sittingProfile;
        [Min(0.1f), Tooltip("Минимальная длина маршрута до случайной цели по NavMesh, в метрах.")]
        [SerializeField] private float _minTravelDistance = 0.6f;
        [Min(0.1f), Tooltip("Максимальная длина маршрута до случайной цели по NavMesh, в метрах. Учитывает обход препятствий.")]
        [SerializeField] private float _maxTravelDistance = 10f;
        [Min(0.1f), Tooltip("Пауза в секундах перед поиском следующей цели после перехода в раздумье. При неудаче поиск повторяется через тот же интервал.")]
        [SerializeField] private float _decisionDelay = 4.5f;
        [Min(0.01f), Tooltip("Дополнительный допуск к Stopping Distance агента, при котором цель считается достигнутой, в метрах.")]
        [SerializeField] private float _arrivalDistance = 0.12f;

        private const int DestinationAttempts = 8;
        private const float NavMeshAttachDistance = 0.6f;
        private const float DestinationSampleDistance = 0.5f;

        private SparkInteractable _spark;
        private NavMeshAgent _agent;
        private NavMeshPath _path;
        private SparkWorldVisual _worldVisual;
        private float _nextDecisionTime;
        private float _restDecisionTime;
        private bool _finishSittingOnClipEnd;
        private int _lieCycles;
        private RestPhase _restPhase;

        private enum RestPhase
        {
            None,
            SitEntering,
            Sitting,
            SitExiting,
            LieEntering,
            LieStar,
            LiePeace,
            LieExiting
        }

        private void OnValidate()
        {
            _minTravelDistance = Mathf.Max(0.1f, _minTravelDistance);
            _maxTravelDistance = Mathf.Max(_minTravelDistance, _maxTravelDistance);
        }

        private void Awake()
        {
            _spark = GetComponent<SparkInteractable>();
            _worldVisual = GetComponent<SparkWorldVisual>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.enabled = false;
            _path = new NavMeshPath();
        }

        private void Start()
        {
            _spark.StateChanged += HandleStateChanged;
            HandleStateChanged(_spark.State);
        }

        private void OnDestroy()
        {
            if (_spark != null)
            {
                _spark.StateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (_spark.State == SparkBehaviourState.Moving)
            {
                if (!_agent.enabled || !_agent.isOnNavMesh)
                {
                    _spark.CompleteMoving();
                    return;
                }

                if (!_agent.pathPending &&
                    (!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete ||
                     _agent.remainingDistance <= _agent.stoppingDistance + _arrivalDistance))
                {
                    _spark.CompleteMoving();
                }

                return;
            }

            if (_spark.State == SparkBehaviourState.Resting)
            {
                UpdateResting();
                return;
            }

            if (_spark.State != SparkBehaviourState.Standing || Time.time < _nextDecisionTime)
            {
                return;
            }

            _nextDecisionTime = Time.time + _decisionDelay;
            SparkAnimatorView animatorView = _worldVisual != null ? _worldVisual.AnimatorView : null;
            if (animatorView != null && Random.value < 0.5f)
            {
                _restPhase = RestPhase.SitEntering;
                if (_spark.TryStartResting())
                {
                    animatorView.PlaySitStart();
                    return;
                }

                _restPhase = RestPhase.None;
            }

            if (!TryAttachAgent())
            {
                return;
            }

            for (int attempt = 0; attempt < DestinationAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * _maxTravelDistance;
                Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit,
                        DestinationSampleDistance, _agent.areaMask) ||
                    !_agent.CalculatePath(hit.position, _path) ||
                    _path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float pathLength = 0f;
                Vector3 previous = transform.position;
                foreach (Vector3 corner in _path.corners)
                {
                    pathLength += Vector3.Distance(previous, corner);
                    previous = corner;
                }

                if (pathLength < _minTravelDistance || pathLength > _maxTravelDistance ||
                    !_spark.TryStartMoving())
                {
                    continue;
                }

                if (!_agent.SetPath(_path))
                {
                    _spark.CompleteMoving();
                }

                return;
            }
        }

        private void HandleStateChanged(SparkBehaviourState state)
        {
            if (state != SparkBehaviourState.Resting)
            {
                _restPhase = RestPhase.None;
            }

            if (state == SparkBehaviourState.Moving)
            {
                return;
            }

            if (_agent.enabled)
            {
                if (_agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                }

                _agent.enabled = false;
            }

            if (state == SparkBehaviourState.Standing)
            {
                _nextDecisionTime = Time.time + _decisionDelay;
            }
            else if (state == SparkBehaviourState.Resting && _restPhase == RestPhase.None)
            {
                SparkAnimatorView view = _worldVisual != null ? _worldVisual.AnimatorView : null;
                if (view != null)
                {
                    EnterSitting(view);
                }
            }
        }

        private void UpdateResting()
        {
            SparkAnimatorView view = _worldVisual != null ? _worldVisual.AnimatorView : null;
            if (view == null)
            {
                _spark.CompleteResting();
                return;
            }

            switch (_restPhase)
            {
                case RestPhase.SitEntering:
                    if (view.IsRestClipComplete())
                    {
                        EnterSitting(view);
                    }
                    break;

                case RestPhase.Sitting:
                    if (_finishSittingOnClipEnd
                        ? !view.IsRestClipComplete()
                        : Time.time < _restDecisionTime)
                    {
                        break;
                    }

                    if (Random.value < 0.5f)
                    {
                        _restPhase = RestPhase.SitExiting;
                        view.PlaySitToDefault();
                    }
                    else
                    {
                        _restPhase = RestPhase.LieEntering;
                        view.PlaySitToLie();
                    }
                    break;

                case RestPhase.SitExiting:
                    if (view.IsRestClipComplete())
                    {
                        _spark.CompleteResting();
                    }
                    break;

                case RestPhase.LieEntering:
                    if (view.IsRestClipComplete())
                    {
                        _lieCycles = 0;
                        _restPhase = RestPhase.LieStar;
                        view.PlayLieStar();
                    }
                    break;

                case RestPhase.LieStar:
                    if (view.IsRestClipComplete())
                    {
                        _restPhase = RestPhase.LiePeace;
                        view.PlayLiePeace();
                    }
                    break;

                case RestPhase.LiePeace:
                    if (view.IsRestClipComplete())
                    {
                        _lieCycles++;
                        if (_lieCycles >= 2)
                        {
                            _restPhase = RestPhase.LieExiting;
                            view.PlayLieToSit();
                        }
                        else
                        {
                            _restPhase = RestPhase.LieStar;
                            view.PlayLieStar();
                        }
                    }
                    break;

                case RestPhase.LieExiting:
                    if (view.IsRestClipComplete())
                    {
                        EnterSitting(view);
                    }
                    break;
            }
        }

        private void EnterSitting(SparkAnimatorView view)
        {
            if (_sittingProfile == null || !_sittingProfile.TryChoose(out SparkSittingProfile.Variant variant))
            {
                Debug.LogError("Для Искры не назначен профиль с доступными вариантами сидения.", this);
                _spark.CompleteResting();
                return;
            }

            float endTime = variant.FinishOnClipEnd
                ? 0f
                : Time.time + variant.ChooseDuration();
            if (!view.PlaySitting(variant, endTime))
            {
                _spark.CompleteResting();
                return;
            }

            _restDecisionTime = endTime;
            _finishSittingOnClipEnd = variant.FinishOnClipEnd;
            _restPhase = RestPhase.Sitting;
        }

        private bool TryAttachAgent()
        {
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                return true;
            }

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit,
                    NavMeshAttachDistance, _agent.areaMask))
            {
                return false;
            }

            _agent.enabled = true;
            if (!_agent.isOnNavMesh || !_agent.Warp(hit.position))
            {
                _agent.enabled = false;
                return false;
            }

            return true;
        }
    }
}
