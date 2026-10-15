using ProjectSparks.Player;
using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkAnimatorView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private SkinnedMeshRenderer _headRenderer;
        [MinTooltip(0.1f, "Максимальное расстояние до игрока или другой Искры, за которыми сидящая Искра может следить взглядом, в метрах.")]
        [SerializeField] private float _lookDistance = 4f;
        [MinTooltip(0f, "Предельный угол поворота головы от направления тела в статическом сидении, в градусах.")]
        [SerializeField] private float _maxLookAngle = 30f;
        [MinTooltip(0f, "Предельный наклон головы вверх и вниз в статическом сидении, в градусах.")]
        [SerializeField] private float _maxLookPitch = 20f;
        [Tooltip("На сколько метров выше выбранной точки Искра направляет взгляд.")]
        [SerializeField] private float _lookHeightOffset = 0.08f;
        [MinTooltip(0.1f, "Скорость плавного поворота головы к цели и обратно, в градусах в секунду.")]
        [SerializeField] private float _lookTurnSpeed = 90f;
        [MinTooltip(0.05f, "За сколько секунд до окончания статического сидения голова плавно вернётся к позе анимации.")]
        [SerializeField] private float _lookReturnDuration = 0.45f;

        private SparkInteractable _spark;
        private Transform _headBone;
        private Transform _lookTarget;
        private SparkAnimatorView _lookTargetSpark;
        private float _nextLookScanTime;
        private float _lookYaw;
        private float _lookPitch;
        private float _sittingEndTime;
        private bool _sittingFinishesOnClipEnd;
        private float _returnStartYaw;
        private float _returnStartPitch;
        private bool _isReturningLook;
        private bool _sittingLookAllowed;
        private bool _isBaby;
        private bool _started;

        private static readonly int DecisionMain = Animator.StringToHash("Base Layer.Decision_Main");
        private static readonly int DecisionBaby = Animator.StringToHash("Base Layer.Decision_Baby");
        private static readonly int MoveMain = Animator.StringToHash("Base Layer.Move_Main");
        private static readonly int MoveBaby = Animator.StringToHash("Base Layer.Move_Baby");
        private static readonly int SitStartMain = Animator.StringToHash("Base Layer.SitStart_Main");
        private static readonly int SitStartBaby = Animator.StringToHash("Base Layer.SitStart_Baby");
        private static readonly int SitToDefaultMain = Animator.StringToHash("Base Layer.Sit_To_Default_Main");
        private static readonly int SitToDefaultBaby = Animator.StringToHash("Base Layer.Sit_To_Default_Baby");
        private static readonly int SitToLie = Animator.StringToHash("Base Layer.Sit_To_Lie");
        private static readonly int LieStar = Animator.StringToHash("Base Layer.Lie_Star");
        private static readonly int LiePeace = Animator.StringToHash("Base Layer.Lie_Peace");
        private static readonly int LieToSit = Animator.StringToHash("Base Layer.Lie_To_Sit");
        private static readonly int Held = Animator.StringToHash("Held");
        private static readonly int Eating = Animator.StringToHash("Eating");
        private static readonly int EditingSit = Animator.StringToHash("Base Layer.SparkRig|Ilde_Sit");

        public void SetGrowth(float growth)
        {
            bool isBaby = growth < 0.35f;
            if (_isBaby == isBaby)
            {
                return;
            }

            _isBaby = isBaby;
            if (!_started)
            {
                return;
            }

            if (_spark == null || _spark.State == SparkBehaviourState.Standing)
            {
                PlayDecision();
            }
            else if (_spark.State == SparkBehaviourState.Moving)
            {
                PlayMoving();
            }
        }

        private void Start()
        {
            if (_animator == null)
            {
                Debug.LogError(
                    "SparkAnimatorView: не назначен Animator.",
                    this);

                enabled = false;
                return;
            }

            _spark = GetComponentInParent<SparkInteractable>();
            if (_headRenderer != null)
            {
                foreach (Transform bone in _headRenderer.bones)
                {
                    if (bone != null && bone.name == "Head")
                    {
                        _headBone = bone;
                        break;
                    }
                }
            }

            _started = true;

            if (_spark == null)
            {
                // В поездке визуал также получает возраст, но не имеет состояния взаимодействия.
                PlayDecision();
                return;
            }

            _spark.StateChanged += OnStateChanged;
            ApplyState(_spark.State);
        }

        private void OnDestroy()
        {
            if (_spark != null)
            {
                _spark.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(SparkBehaviourState state)
        {
            ApplyState(state);
        }

        private void ApplyState(SparkBehaviourState state)
        {
            _animator.ResetTrigger(Held);
            _animator.ResetTrigger(Eating);

            switch (state)
            {
                case SparkBehaviourState.Standing:
                    PlayDecision();
                    break;

                case SparkBehaviourState.Moving:
                    PlayMoving();
                    break;

                case SparkBehaviourState.Held:
                    _animator.SetTrigger(Held);
                    break;

                case SparkBehaviourState.Eating:
                    _animator.SetTrigger(Eating);
                    break;

                case SparkBehaviourState.Editing:
                    _animator.CrossFade(EditingSit, 0.1f);
                    break;
            }
        }

        private int _activeRestState;

        public void PlaySitStart() => PlayRest(_isBaby ? SitStartBaby : SitStartMain);
        public void PlaySitToDefault() => PlayRest(_isBaby ? SitToDefaultBaby : SitToDefaultMain);
        public bool PlaySitting(SparkSittingProfile.Variant variant, float endTime)
        {
            if (variant == null || string.IsNullOrWhiteSpace(variant.AnimatorState))
            {
                return false;
            }

            int state = Animator.StringToHash(variant.AnimatorState);
            if (!_animator.HasState(0, state))
            {
                Debug.LogError($"В Animator не найден вариант сидения '{variant.AnimatorState}'.", this);
                return false;
            }

            PlayRest(state);
            _sittingEndTime = endTime;
            _sittingFinishesOnClipEnd = variant.FinishOnClipEnd;
            _sittingLookAllowed = variant.LookAtTargets;
            _isReturningLook = false;
            return true;
        }
        public void PlaySitToLie() => PlayRest(SitToLie);
        public void PlayLieStar() => PlayRest(LieStar);
        public void PlayLiePeace() => PlayRest(LiePeace);
        public void PlayLieToSit() => PlayRest(LieToSit);

        public bool IsRestClipComplete()
        {
            if (_activeRestState == 0 || _animator.IsInTransition(0))
            {
                return false;
            }

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.fullPathHash == _activeRestState && state.normalizedTime >= 1f;
        }

        private void LateUpdate()
        {
            if (_headBone == null)
            {
                return;
            }

            if (_spark == null || _spark.State != SparkBehaviourState.Resting ||
                !_sittingLookAllowed || _animator.IsInTransition(0))
            {
                _lookYaw = 0f;
                _lookPitch = 0f;
                _isReturningLook = false;
                return;
            }

            AnimatorStateInfo sittingState = _animator.GetCurrentAnimatorStateInfo(0);
            if (sittingState.fullPathHash != _activeRestState)
            {
                _lookYaw = 0f;
                _lookPitch = 0f;
                _isReturningLook = false;
                return;
            }

            float remainingSitTime = _sittingFinishesOnClipEnd
                ? Mathf.Max(0f, 1f - sittingState.normalizedTime) * sittingState.length
                : _sittingEndTime - Time.time;
            if (remainingSitTime <= _lookReturnDuration)
            {
                if (!_isReturningLook)
                {
                    _returnStartYaw = _lookYaw;
                    _returnStartPitch = _lookPitch;
                    _isReturningLook = true;
                }

                float progress = 1f - Mathf.Clamp01(remainingSitTime / _lookReturnDuration);
                float easedProgress = progress * progress * (3f - 2f * progress);
                _lookYaw = Mathf.Lerp(_returnStartYaw, 0f, easedProgress);
                _lookPitch = Mathf.Lerp(_returnStartPitch, 0f, easedProgress);
                ApplyHeadLook();
                return;
            }

            if (Time.time >= _nextLookScanTime)
            {
                FindLookTarget();
                _nextLookScanTime = Time.time + 0.25f;
            }

            float targetYaw = 0f;
            float targetPitch = 0f;
            if (_lookTarget != null)
            {
                Vector3 toTarget = GetLookPoint(_lookTarget, _lookTargetSpark) - _headBone.position;
                Vector3 horizontal = Vector3.ProjectOnPlane(toTarget, Vector3.up);
                if (horizontal.sqrMagnitude > 0.0001f)
                {
                    targetYaw = Mathf.Clamp(
                        Vector3.SignedAngle(transform.forward, horizontal, Vector3.up),
                        -_maxLookAngle, _maxLookAngle);
                    targetPitch = Mathf.Clamp(
                        Mathf.Atan2(toTarget.y, horizontal.magnitude) * Mathf.Rad2Deg,
                        -_maxLookPitch, _maxLookPitch);
                }
            }

            _lookYaw = Mathf.MoveTowards(_lookYaw, targetYaw, _lookTurnSpeed * Time.deltaTime);
            _lookPitch = Mathf.MoveTowards(_lookPitch, targetPitch, _lookTurnSpeed * Time.deltaTime);
            ApplyHeadLook();
        }

        private void ApplyHeadLook()
        {
            Quaternion yawRotation = Quaternion.AngleAxis(_lookYaw, Vector3.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(-_lookPitch, yawRotation * transform.right);
            _headBone.rotation = pitchRotation * yawRotation * _headBone.rotation;
        }

        private void FindLookTarget()
        {
            _lookTarget = null;
            _lookTargetSpark = null;
            float nearestDistanceSquared = _lookDistance * _lookDistance;
            Vector3 origin = _headBone.position;

            Transform playerTarget = PlayerLookTarget.Current;
            if (playerTarget != null)
            {
                ConsiderLookTarget(playerTarget, null, origin, ref nearestDistanceSquared);
            }

            foreach (SparkInteractable otherSpark in SparkInteractable.ActiveSparks)
            {
                if (otherSpark == null || otherSpark == _spark)
                {
                    continue;
                }

                SparkAnimatorView otherView = otherSpark.GetComponent<SparkWorldVisual>()?.AnimatorView;
                if (otherView == null || otherView._headRenderer == null)
                {
                    continue;
                }

                ConsiderLookTarget(otherSpark.transform, otherView, origin, ref nearestDistanceSquared);
            }
        }

        private Vector3 GetLookPoint(Transform target, SparkAnimatorView targetSpark)
        {
            Vector3 point = targetSpark != null && targetSpark._headRenderer != null
                ? targetSpark._headRenderer.bounds.center
                : target.position;
            return point + Vector3.up * _lookHeightOffset;
        }

        private void ConsiderLookTarget(
            Transform target, SparkAnimatorView targetSpark, Vector3 origin, ref float nearestDistanceSquared)
        {
            float distanceSquared = (GetLookPoint(target, targetSpark) - origin).sqrMagnitude;
            if (distanceSquared >= nearestDistanceSquared)
            {
                return;
            }

            nearestDistanceSquared = distanceSquared;
            _lookTarget = target;
            _lookTargetSpark = targetSpark;
        }

        private void PlayRest(int state)
        {
            _activeRestState = state;
            _sittingLookAllowed = false;
            _animator.CrossFade(state, 0.1f);
        }

        private void PlayDecision()
        {
            _animator.CrossFade(_isBaby ? DecisionBaby : DecisionMain, 0.1f);
        }

        private void PlayMoving()
        {
            _animator.CrossFade(_isBaby ? MoveBaby : MoveMain, 0.1f);
        }
    }
}
