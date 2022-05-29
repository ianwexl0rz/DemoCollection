using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ActorFramework;
using JetBrains.Annotations;
using UnityEngine;
using Noesis;

namespace DemoCollection.DataBinding
{
    //[CreateAssetMenu (menuName = "Noesis/HUD Controller", fileName = "HUD Controller")]
    [Serializable]
    public class HudBinding : ObservableObject
    {
        [SerializeField] private List<Trackable> _npcHealthBars;

        private PlayerController _playerController;
        private Trackable _trackedTarget;

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
        
        private Health _health;
        public Health Health
        {
            get => _health;
            private set => SetProperty(ref _health, value);
        }
        
        private Health _enemyHealth;
        public Health EnemyHealth
        {
            get => _enemyHealth;
            private set => SetProperty(ref _enemyHealth, value);
        }

        public List<Trackable> RecentlyHit => _npcHealthBars;

        public HudBinding(PlayerController playerController)
        {
            _playerController = playerController;
            
            PlayerController.PossessedActor += RegisterActor;
            PlayerController.ChangedRecentlyHitList += UpdateVisibleHealthBars;
            LockOn.SetIndicatorData += SetLockOnIndicator;
            LockOn.TargetChanged += RegisterTarget;
        }

        private void RegisterActor(Actor actor)
        {
            Health = actor.Health;
            _playerController.RecentlyHit.Remove(actor.Trackable);
            RegisterTarget(actor.TrackedTarget);
        }

        private void RegisterTarget(Trackable newTarget)
        {
            _trackedTarget = newTarget;
            UpdateVisibleHealthBars();
        }
        
        private void UpdateVisibleHealthBars()
        {
            _npcHealthBars = new List<Trackable>(_playerController.RecentlyHit.Keys);
            if (_trackedTarget && !_npcHealthBars.Contains(_trackedTarget))
                _npcHealthBars.Add(_trackedTarget);
            OnPropertyChanged("RecentlyHit");
        }

        private void SetLockOnIndicator(Trackable trackable)
        {
            if (trackable != null)
            {
            	HasTarget = true;
            	TargetX = trackable.ScreenPosX;
            	TargetY = trackable.ScreenPosY;
            }
            else
            {
            	HasTarget = false;
            	TargetX = 0;
            	TargetY = 0;
            }
        }
    }
}