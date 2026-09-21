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
        private SparkInteractable _spark;
        private GameObject _instance;
        [Inject]
        private void Construct(SparkVisualFactory factory, ISparkProgressService progress)
        { _factory = factory; _progress = progress; }
        private void Start()
        {
            _spark = GetComponent<SparkInteractable>();
            _progress.SparkChanged += Refresh;
            Refresh(_spark.Id);
        }
        private void Refresh(string id)
        {
            if (id != _spark.Id) return;
            SparkProgressData data = _progress.GetProgress(id);
            if (_instance == null) _instance = _factory.Create(data, transform);
            else _factory.Apply(_instance, data);
        }
        private void OnDestroy()
        {
            if (_progress != null) _progress.SparkChanged -= Refresh;
            if (_instance != null) Destroy(_instance);
        }
    }
}
