using System.Collections.Generic;
using UnityEngine;

namespace ProjectSparks.Companions
{
    [DisallowMultipleComponent]
    public sealed class SparkFocusOutline : MonoBehaviour
    {
        [SerializeField] private Material _outlineMaterial;

        private readonly List<SkinnedMeshRenderer> _outlines = new();

        private void OnEnable()
        {
            BuildIfNeeded();
            SetVisible(true);
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        private void OnDestroy()
        {
            foreach (SkinnedMeshRenderer outline in _outlines)
            {
                if (outline != null)
                {
                    Destroy(outline.gameObject);
                }
            }

            _outlines.Clear();
        }

        private void BuildIfNeeded()
        {
            if (_outlines.Count > 0 || _outlineMaterial == null)
            {
                return;
            }

            SparkWorldVisual worldVisual = GetComponentInParent<SparkWorldVisual>();

            if (worldVisual == null)
            {
                return;
            }

            SkinnedMeshRenderer[] sources = worldVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer source in sources)
            {
                if (source == null ||
                    source.sharedMesh == null ||
                    source.name == "SparkEyes")
                {
                    continue;
                }

                GameObject outlineObject = new GameObject(source.name + "_Outline");

                outlineObject.transform.SetParent(source.transform, false);
                outlineObject.layer = source.gameObject.layer;

                SkinnedMeshRenderer outline = outlineObject.AddComponent<SkinnedMeshRenderer>();

                outline.sharedMesh = source.sharedMesh;
                outline.rootBone = source.rootBone;
                outline.bones = source.bones;
                outline.localBounds = source.localBounds;
                outline.quality = source.quality;
                outline.updateWhenOffscreen = source.updateWhenOffscreen;

                int materialCount = Mathf.Max(
                    1,
                    source.sharedMesh.subMeshCount);

                Material[] materials = new Material[materialCount];

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = _outlineMaterial;
                }

                outline.sharedMaterials = materials;

                _outlines.Add(outline);
            }
        }

        private void SetVisible(bool visible)
        {
            foreach (SkinnedMeshRenderer outline in _outlines)
            {
                if (outline != null)
                {
                    outline.enabled = visible;
                }
            }
        }
    }
}