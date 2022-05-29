using System;
using System.Linq;
using UnityEngine;

namespace ActorFramework
{
    public class TrackableCapsule : Trackable
    {
        private CapsuleCollider _capsuleCollider;

        protected override void Awake()
        {
            base.Awake();
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }

        public override Vector3 GetEyesPosition()
        {
            return transform.TransformPoint(_capsuleCollider.center) + new Vector3(0, _capsuleCollider.bounds.extents.y, 0);
        }

        public override Vector3 GetGroundPosition()
        {
           return transform.TransformPoint(_capsuleCollider.center) - new Vector3(0, _capsuleCollider.bounds.extents.y, 0);
        }

        public override Vector3 GetCenter() => transform.TransformPoint(_capsuleCollider.center);

        public override float GetHeight() => _capsuleCollider.height;
    }
}