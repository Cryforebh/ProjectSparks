using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkEmotionView : MonoBehaviour
    {
        [Tooltip("Рендерер головы Искры с Blend Shape Sad и Angry.")]
        [SerializeField] private SkinnedMeshRenderer _head;

        private int _sadIndex = -1;
        private int _angryIndex = -1;

        private SparkEmotion _emotion = SparkEmotion.Neutral;

        private void Awake()
        {
            CacheBlendShapes();
        }

        public void SetEmotion(SparkEmotion emotion)
        {
            _emotion = emotion;
            ApplyEmotion();
        }

        private void LateUpdate()
        {
            ApplyEmotion();
        }

        private void ApplyEmotion()
        {
            if (_head == null)
            {
                return;
            }

            if (_sadIndex < 0 || _angryIndex < 0)
            {
                CacheBlendShapes();
            }

            if (_sadIndex >= 0)
            {
                _head.SetBlendShapeWeight(
                    _sadIndex,
                    _emotion == SparkEmotion.Sad ? 100f : 0f);
            }

            if (_angryIndex >= 0)
            {
                _head.SetBlendShapeWeight(
                    _angryIndex,
                    _emotion == SparkEmotion.Angry ? 100f : 0f);
            }
        }

        private void CacheBlendShapes()
        {
            if (_head == null || _head.sharedMesh == null)
            {
                return;
            }

            _sadIndex = _head.sharedMesh.GetBlendShapeIndex("Sad");
            _angryIndex = _head.sharedMesh.GetBlendShapeIndex("Angry");
        }
    }
}
