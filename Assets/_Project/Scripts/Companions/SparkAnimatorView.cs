using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkAnimatorView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private SparkInteractable _spark;

        private static readonly int Standing = Animator.StringToHash("Standing");
        private static readonly int Held = Animator.StringToHash("Held");
        private static readonly int Eating = Animator.StringToHash("Eating");

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

            if (_spark == null)
            {
                // ¬ поездке SparkVisual существует без SparkInteractable.
                // ќставл€ем Animator в его стандартном состо€нии.
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
            _animator.ResetTrigger(Standing);
            _animator.ResetTrigger(Held);
            _animator.ResetTrigger(Eating);

            switch (state)
            {
                case SparkBehaviourState.Standing:
                    _animator.SetTrigger(Standing);
                    break;

                case SparkBehaviourState.Held:
                    _animator.SetTrigger(Held);
                    break;

                case SparkBehaviourState.Eating:
                    _animator.SetTrigger(Eating);
                    break;
            }
        }
    }
}
