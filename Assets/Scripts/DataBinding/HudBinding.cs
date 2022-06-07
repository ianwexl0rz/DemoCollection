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
    [Serializable]
    public class HudBinding : ObservableObject
    {
        private List<Trackable> _npcHealthBars;
        private Trackable _trackedTarget;
        private Dictionary<Trackable, float> _recentlyHit;

        public bool HasTarget => _trackedTarget != null;
        public float TargetX => HasTarget ? _trackedTarget.ScreenPosX : 0;
        public float TargetY => HasTarget ? _trackedTarget.ScreenPosY : 0;
        public Health EnemyHealth => HasTarget ? _trackedTarget.Health : null;

        private Health _health;
        public Health Health
        {
            get => _health;
            private set => SetProperty(ref _health, value);
        }
        
        private Stamina _stamina;
        public Stamina Stamina
        {
            get => _stamina;
            private set => SetProperty(ref _stamina, value);
        }

        public List<Trackable> RecentlyHit => _npcHealthBars;

        public HudBinding(PlayerController playerController)
        {
            _recentlyHit = playerController.RecentlyHit;
            
            playerController.PossessedActor += RegisterActor;
            playerController.TargetChanged += RegisterTarget;
            playerController.ChangedRecentlyHitList += UpdateVisibleHealthBars;
            playerController.RequestUpdateReticle += UpdateReticle;
        }

        private void RegisterActor(Actor actor)
        {
            Health = actor.Health;
            Stamina = actor.Stamina;
            UpdateVisibleHealthBars();
        }

        private void RegisterTarget(Trackable newTarget)
        {
            _trackedTarget = newTarget;
            UpdateVisibleHealthBars();
        }
        
        private void UpdateVisibleHealthBars()
        {
            _npcHealthBars = new List<Trackable>(_recentlyHit.Keys);
            if (_trackedTarget && !_npcHealthBars.Contains(_trackedTarget))
                _npcHealthBars.Add(_trackedTarget);
            OnPropertyChanged("RecentlyHit");
        }

        private void UpdateReticle(Trackable trackable)
        {
            OnPropertyChanged("HasTarget");
            OnPropertyChanged("TargetX");
            OnPropertyChanged("TargetY");
        }
    }
}