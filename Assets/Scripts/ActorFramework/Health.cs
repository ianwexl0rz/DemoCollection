using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DemoCollection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace ActorFramework
{
    [RequireComponent(typeof(Entity))]
    public class Health : EntityResource
    {
        private void OnEnable()
        {
            Entity.GetHit += HandleGetHit;
        }

        private void OnDisable()
        {
            Entity.GetHit -= HandleGetHit;
        }

        public void HandleGetHit(CombatEvent combatEvent)
        {
            var damage = combatEvent.AttackData.damage;
            TakeDamage(damage);
        }

        public void TakeDamage(int damage)
        {
            SetEcho();
            ApplyChange(-damage);
        }
    }
}