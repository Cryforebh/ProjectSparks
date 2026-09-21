using System;
using UnityEngine;

namespace ProjectSparks.SparkPreview
{
    public sealed class SparkPreviewProfile : ScriptableObject
    {
        [Tooltip("Клипы отдельного предпросмотра.")][SerializeField] private PreviewClip[] _clips;
        [Tooltip("Локальные значения из импортированных калибровок.")][SerializeField] private BoneCalibration[] _bones;
        public PreviewClip[] Clips => _clips;
        public BoneCalibration[] Bones => _bones;
        public void Initialize(PreviewClip[] clips, BoneCalibration[] bones) { _clips = clips; _bones = bones; }
    }

    [Serializable] public sealed class PreviewClip
    {
        public string name;
        public AnimationClip clip;
        public bool loop;
        public GroundSample[] ground;
    }
    [Serializable] public sealed class GroundSample { public float time, h, b, m; }
    [Serializable] public struct LocalPose
    {
        public Vector3 position, scale;
        public Quaternion rotation;
        public static LocalPose Read(Transform t) => new LocalPose { position = t.localPosition, rotation = t.localRotation, scale = t.localScale };
        public static LocalPose Blend(LocalPose a, LocalPose b, float t) => new LocalPose
        { position = Vector3.Lerp(a.position, b.position, t), scale = Vector3.Lerp(a.scale, b.scale, t), rotation = Quaternion.Slerp(a.rotation, b.rotation, t) };
    }
    [Serializable] public sealed class BoneCalibration
    {
        public string path, kind;
        public bool position, rotation, scale;
        public LocalPose[] samples;
        public LocalPose Evaluate(float growth, float development, float rest, float antenna)
        {
            if (kind == "Ears")
            {
                float x = Mathf.Clamp01(development) * 20, y = Mathf.Clamp01(rest) * 20;
                int a = Mathf.Min((int)x, 19), b = Mathf.Min((int)y, 19);
                return LocalPose.Blend(LocalPose.Blend(samples[a * 21 + b], samples[(a + 1) * 21 + b], x - a),
                    LocalPose.Blend(samples[a * 21 + b + 1], samples[(a + 1) * 21 + b + 1], x - a), y - b);
            }
            float parameter = kind == "Antenna" ? Mathf.Lerp(antenna, 1, growth) : growth;
            float index = Mathf.Clamp01(parameter) * (samples.Length - 1);
            int first = Mathf.Min((int)index, samples.Length - 2);
            return LocalPose.Blend(samples[first], samples[first + 1], index - first);
        }
    }
}
