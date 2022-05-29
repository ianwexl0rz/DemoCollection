using System;
using System.Linq;
using UnityEngine;

namespace ActorFramework
{
    public class TrackableSphere : Trackable
    {
        private SphereCollider _sphereCollider;

        protected override void Awake()
        {
            base.Awake();
            _sphereCollider = GetComponent<SphereCollider>();
        }

        public override Vector3 GetEyesPosition()
        {
            return transform.TransformPoint(_sphereCollider.center) + new Vector3(0, _sphereCollider.bounds.extents.y, 0);
        }

        public override Vector3 GetGroundPosition()
        {
            return transform.TransformPoint(_sphereCollider.center) - new Vector3(0, _sphereCollider.bounds.extents.y, 0);
        }

        public override Vector3 GetCenter() => transform.TransformPoint(_sphereCollider.center);

        public override float GetHeight() => _sphereCollider.radius * 2;
    }
}