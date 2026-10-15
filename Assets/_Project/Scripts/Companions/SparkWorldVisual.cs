using System;
using Cysharp.Threading.Tasks;
using ProjectSparks.Core;
using UnityEngine;
using Zenject;
namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SparkInteractable))]
    public sealed class SparkWorldVisual : MonoBehaviour
    {
        private SparkVisualFactory _factory;
        private ISparkProgressService _progress;
        private IProgressService _progressService;
        private SparkInteractable _spark;
        private GameObject _instance;
        private SparkFruitAttachment _fruitAttachment;
        public SparkAnimatorView AnimatorView => _instance != null ? _instance.GetComponent<SparkAnimatorView>() : null;
        public Transform FruitHoldAnchor => _fruitAttachment != null
            ? _fruitAttachment.Anchor
            : null;
        [Inject]
        private void Construct(
            SparkVisualFactory factory,
            ISparkProgressService progress,
            IProgressService progressService)
        {
            _factory = factory;
            _progress = progress;
            _progressService = progressService;
        }

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                await _progressService.LoadAsync(this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return;
            }

            _spark = GetComponent<SparkInteractable>();
            _progress.SparkChanged += Refresh;
            Refresh(_spark.Id);
        }
        private void Refresh(string id)
        {
            if (id != _spark.Id) return;
            SparkProgressData data = _progress.GetProgress(id);
            if (_instance == null)
            {
                _instance = _factory.Create(data, transform);
                _fruitAttachment = _instance.GetComponent<SparkFruitAttachment>();
            }
            else _factory.Apply(_instance, data);
        }
        private void OnDestroy()
        {
            if (_progress != null) _progress.SparkChanged -= Refresh;
            if (_instance != null) Destroy(_instance);
        }
    }
}
