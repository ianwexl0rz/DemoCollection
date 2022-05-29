using System;
using UnityEngine;

namespace ActorFramework
{
    [Serializable]
    public struct RigidbodyState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public bool isKinematic;

        public RigidbodyState(Rigidbody rb)
        {
            position = rb.position;
            rotation = rb.rotation;
            velocity = rb.velocity;
            angularVelocity = rb.angularVelocity;
            isKinematic = rb.isKinematic;
        }
    }
    
    public class EntityPhysics : MonoBehaviour
    {
        private RigidbodyState _savedState;
        
        public Rigidbody Rigidbody { get; private set; }
        public Entity Entity { get; private set; }

        protected virtual void OnEnable()
        {
            Entity = GetComponent<Entity>();
            Rigidbody = GetComponent<Rigidbody>();
            Entity.SetPaused += HandleSetPaused;
            Entity.GetHit += HandleGetHit;
        }
        
        protected virtual void OnDisable()
        {
            Entity.SetPaused -= HandleSetPaused;
            Entity.GetHit -= HandleGetHit;
        }

        private void HandleSetPaused(bool value)
        {
            if (Rigidbody == null) return;
            
            if (value)
            {
                _savedState = new RigidbodyState(Rigidbody);
                Rigidbody.isKinematic = true;
                Rigidbody.velocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
                Rigidbody.position = transform.position;
                Rigidbody.rotation = transform.rotation;
                Rigidbody.Sleep();
            }
            else
            {
                Rigidbody.WakeUp();
                Rigidbody.RestoreState(_savedState);
            }
        }

        private void HandleGetHit(CombatEvent combatEvent)
        {
            var (instigator, target, point, direction, attackData) = combatEvent;
            var velocity = direction * (attackData.knockback / Time.fixedDeltaTime);
            Rigidbody.AddForceAtPosition(velocity, point, ForceMode.Impulse);
        }
    }
}