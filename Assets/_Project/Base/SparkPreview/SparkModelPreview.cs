using UnityEngine;

namespace ProjectSparks.SparkPreview
{
    [DisallowMultipleComponent]
    public sealed class SparkModelPreview : MonoBehaviour
    {
        [Tooltip("Профиль импортированных клипов и калибровок.")][SerializeField] private SparkPreviewProfile _profile;
        [Tooltip("Корень экземпляра FBX.")][SerializeField] private Transform _model;
        [Tooltip("Номер клипа в профиле.")][SerializeField] private int _clipIndex;
        [Tooltip("Положение внутри клипа.")][Range(0, 1)][SerializeField] private float _normalizedTime;
        [Tooltip("Проигрывать выбранный клип в Play Mode.")][SerializeField] private bool _play = true;
        [Tooltip("Рост от маленькой до взрослой формы.")][Range(0, 1)][SerializeField] private float _growth = 1;
        [Tooltip("Развитие формы ушей.")][Range(0, 1)][SerializeField] private float _earDevelopment = 1;
        [Tooltip("Положение ушей в покое.")][Range(0, 1)][SerializeField] private float _earRest = 1;
        [Tooltip("Размер усиков маленькой Искры.")][Range(0.1f, 1)][SerializeField] private float _antennaBabyScale = 0.35f;
        [Tooltip("Грустное выражение мордочки.")][Range(0, 1)][SerializeField] private float _sad;
        [Tooltip("Злое выражение мордочки.")][Range(0, 1)][SerializeField] private float _angry;
        private Transform[] _targets;
        private Transform _rig;
        private SkinnedMeshRenderer _head, _ears;
        private int _sadIndex, _angryIndex, _earIndex;
        public SparkPreviewProfile Profile => _profile;
        public Transform Model => _model;
        public void Initialize(SparkPreviewProfile profile, Transform model) { _profile = profile; _model = model; _targets = null; }
        private void OnEnable() { if (_profile != null) Evaluate(); }
        private void Update()
        {
            if (_profile == null) return;
            var clip = _profile.Clips[Mathf.Clamp(_clipIndex, 0, _profile.Clips.Length - 1)];
            if (_play && clip.clip.length > 0)
            {
                _normalizedTime += Time.deltaTime / clip.clip.length;
                _normalizedTime = clip.loop ? Mathf.Repeat(_normalizedTime, 1) : Mathf.Clamp01(_normalizedTime);
            }
            Evaluate();
        }
        private void Cache()
        {
            if (_targets != null) return;
            _targets = new Transform[_profile.Bones.Length];
            for (int i = 0; i < _targets.Length; i++) _targets[i] = _model.Find(_profile.Bones[i].path);
            _rig = _model.Find("SparkRoot/SparkRig");
            foreach (var renderer in _model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == "SparkHead") _head = renderer;
                if (renderer.name == "SparkEars") _ears = renderer;
            }
            _sadIndex = _head.sharedMesh.GetBlendShapeIndex("Sad");
            _angryIndex = _head.sharedMesh.GetBlendShapeIndex("Angry");
            _earIndex = _ears.sharedMesh.GetBlendShapeIndex("EarDevelopment");
        }
        public void SetPreview(int clip, float time, float growth, float development, float rest)
        { _clipIndex = clip; _normalizedTime = time; _growth = growth; _earDevelopment = development; _earRest = rest; Evaluate(); }
        public void Evaluate()
        {
            if (_profile == null || _model == null || _profile.Clips.Length == 0) return;
            Cache();
            var clip = _profile.Clips[Mathf.Clamp(_clipIndex, 0, _profile.Clips.Length - 1)];
            float time = Mathf.Clamp01(_normalizedTime) * clip.clip.length;
            clip.clip.SampleAnimation(_model.gameObject, time);
            for (int i = 0; i < _targets.Length; i++)
            {
                var bone = _profile.Bones[i];
                var pose = bone.Evaluate(_growth, _earDevelopment, _earRest, _antennaBabyScale);
                if (bone.position) _targets[i].localPosition = pose.position;
                if (bone.rotation) _targets[i].localRotation = pose.rotation;
                if (bone.scale) _targets[i].localScale = pose.scale;
            }
            float sum = Mathf.Max(1, _sad + _angry);
            _head.SetBlendShapeWeight(_sadIndex, _sad / sum * 100);
            _head.SetBlendShapeWeight(_angryIndex, _angry / sum * 100);
            _ears.SetBlendShapeWeight(_earIndex, _earDevelopment * 100);
            var samples = clip.ground;
            if (samples != null && samples.Length > 0)
            {
                int a = Mathf.Min(Mathf.FloorToInt(time * 48), samples.Length - 1), b = Mathf.Min(a + 1, samples.Length - 1);
                float t = samples[b].time > samples[a].time ? Mathf.InverseLerp(samples[a].time, samples[b].time, time) : 0;
                float h = Mathf.Lerp(samples[a].h, samples[b].h, t), low = Mathf.Lerp(samples[a].b, samples[b].b, t), high = Mathf.Lerp(samples[a].m, samples[b].m, t);
                var p = _rig.localPosition;
                // Импортированный SparkRoot поворачивает локальную +Z в мировую -Y.
                p.z = Mathf.Min(h * Mathf.Lerp(0.64473348855972f, 1, _growth), Mathf.Lerp(low, high, _growth));
                _rig.localPosition = p;
            }
        }
    }
}
