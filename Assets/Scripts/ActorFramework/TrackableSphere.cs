using System;
using System.Linq;
using UnityEngine;

namespace ActorFramework
{
    public class TrackableSphere : MonoBehaviour, ITrackable
    {
        public event Action Destroyed;

        private SphereCollider _sphereCollider;
        private Renderer[] _renderers;

        private void Awake()
        {
            _sphereCollider = GetComponent<SphereCollider>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnDestroy() => Destroyed?.Invoke();

        public Vector3 GetEyesPosition()
        {
            return transform.TransformPoint(_sphereCollider.center) + new Vector3(0, _sphereCollider.bounds.extents.y, 0);
        }

        public Vector3 GetGroundPosition()
        {
            return transform.TransformPoint(_sphereCollider.center) - new Vector3(0, _sphereCollider.bounds.extents.y, 0);
        }

        public Vector3 GetCenter() => transform.TransformPoint(_sphereCollider.center);

        public float GetHeight() => _sphereCollider.radius * 2;

        public bool IsVisible() => _renderers.Any(r => r != null && r.isVisible);
    }
}