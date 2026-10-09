using CampusLife.Runtime.Rendering;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>Optional floating marker above the current objective (or the door leading to it).</summary>
    public sealed class GuideBeacon : MonoBehaviour
    {
        Transform _gem;
        Vector3 _target;
        bool _reducedMotion;

        public static GuideBeacon Create(MaterialLibrary materials, Transform parent)
        {
            var go = new GameObject("GuideBeacon");
            go.transform.SetParent(parent, false);
            var beacon = go.AddComponent<GuideBeacon>();
            var gem = MeshCache.CreateVisual("Gem", PrimitiveType.Cube, go.transform, materials.GetEmissive(new Color(1f, 0.78f, 0.15f), 2f));
            gem.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            gem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            gem.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beacon._gem = gem.transform;
            go.SetActive(false);
            return beacon;
        }

        public void SetTarget(Vector3? target, bool reducedMotion)
        {
            _reducedMotion = reducedMotion;
            if (!target.HasValue)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            _target = target.Value;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        void Update()
        {
            float bob = _reducedMotion ? 0f : Mathf.Sin(Time.time * 2f) * 0.15f;
            transform.position = _target + Vector3.up * (2.7f + bob);
            if (!_reducedMotion) _gem.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }
    }
}
