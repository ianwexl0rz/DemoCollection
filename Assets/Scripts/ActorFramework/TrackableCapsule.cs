using System;
using System.Linq;
using UnityEngine;

namespace ActorFramework
{
    public class TrackableCapsule : MonoBehaviour, ITrackable
    {
        public event Action Destroyed;

        private CapsuleCollider _capsuleCollider;
        private Renderer[] _renderers;

        private void Awake()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _renderers = GetComponentsInChildren<Renderer>();
        }
        
        private void OnDestroy() => Destroyed?.Invoke();

        public Vector3 GetEyesPosition()
        {
            return transform.TransformPoint(_capsuleCollider.center) + new Vector3(0, _capsuleCollider.bounds.extents.y, 0);
        }

        public Vector3 GetGroundPosition()
        {
           return transform.TransformPoint(_capsuleCollider.center) - new Vector3(0, _capsuleCollider.bounds.extents.y, 0);
        }

        public Vector3 GetCenter() => transform.TransformPoint(_capsuleCollider.center);

        public float GetHeight() => _capsuleCollider.height;

        public bool IsVisible() => _renderers.Any(r => r != null && r.isVisible);
    }
}