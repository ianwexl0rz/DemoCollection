﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ActorFramework;
using JetBrains.Annotations;
using UnityEngine;

namespace DemoCollection.DataBinding
{
    [CreateAssetMenu (menuName = "Noesis/HUD Controller", fileName = "HUD Controller")]
    public class HUDController : ObservableScriptableObject
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private LockOn lockOn;
        
        private bool _hasTarget;
        public bool HasTarget
        {
            get => _hasTarget;
            private set => SetProperty(ref _hasTarget, value);
        }

        private float _targetX;
        public float TargetX
        {
            get => _targetX;
            private set => SetProperty(ref _targetX, value);
        }

        private float _targetY;
        public float TargetY
        {
            get => _targetY;
            private set => SetProperty(ref _targetY, value);
        }

        [NonSerialized]
        private Health _health;
        public Health Health
        {
            get => _health;
            private set => SetProperty(ref _health, value);
        }
        
        [NonSerialized]
        private Health _enemyHealth;
        public Health EnemyHealth
        {
            get => _enemyHealth;
            private set => SetProperty(ref _enemyHealth, value);
        }
        
        public void Init()
        {
            playerController.SetActor += RegisterActor;
            LockOn.SetIndicatorData += SetLockOnIndicator;
            LockOn.TargetChanged += RegisterTarget;
        }

        private void RegisterActor(Actor actor) => Health = actor.Health;

        private void RegisterTarget(ITrackable trackable)
        {
            if (trackable == null) return;
            
            var enemyHealth = ((UnityEngine.Component)trackable).GetComponent<Health>();
            if (enemyHealth != null)
            {
                EnemyHealth = enemyHealth;
            }
        }

        private void SetLockOnIndicator(LockOn.IndicatorData indicatorData)
        {
            HasTarget = indicatorData.HasTarget;
            TargetX = indicatorData.TargetX;
            TargetY = indicatorData.TargetY;
        }
    }
}