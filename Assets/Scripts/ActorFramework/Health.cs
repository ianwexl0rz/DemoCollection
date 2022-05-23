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
    public class Health : MonoBehaviour, INotifyPropertyChanged
    {
        public event Action Depleted;

        [SerializeField] private int _current = 100;
        [SerializeField] private int _maximum = 100;
        [SerializeField] private UnityEvent OnDepleted = new UnityEvent();

        private Entity _entity;

        public int Current
        {
            get => _current;
            set { if (_current != value) { _current = value; OnPropertyChanged("Current"); } }
        }

        public int Maximum
        {
            get => _maximum;
            set { if (_maximum != value) { _maximum = value; OnPropertyChanged("Maximum"); } }
        }

        private void Awake()
        {
            _entity = GetComponent<Entity>();
            _entity.GetHit += OnGetHit;
        }

        private void OnDestroy()
        {
            _entity.GetHit -= OnGetHit;
        }

        public void ApplyChange(int delta)
        {
            // Calculate new health (un-clamped so we can do "overkill" events, etc.)
            var newValue = _current + delta;

            // Clamp new health.
            newValue = Mathf.Clamp(newValue, 0, _maximum);
        
            // Early out if no change...
            if (newValue == _current) return;

            // Update health.
            Current = newValue;
        
            // Destroy if health is zero.
            if (newValue < Mathf.Epsilon)
            {
                Depleted?.Invoke();
                OnDepleted.Invoke();
            }
        }

        public void OnGetHit(CombatEvent combatEvent)
        {
            var damage = (int)combatEvent.AttackData.damage;
            ApplyChange(-damage);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}