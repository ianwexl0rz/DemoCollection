using System;
using System.Collections;
using DemoCollection.DataBinding;
using UnityEngine;
using UnityEngine.Events;

namespace ActorFramework
{
    public class EntityResource : ObservableMonobehaviour
    {
        private const float EchoLifespan = 0.5f;
        private const float EchoResetRate = 0.5f;
        
        public event Action Depleted;

        [SerializeField] private int _current = 100;
        [SerializeField] private int _maximum = 100;
        [SerializeField] private UnityEvent OnDepleted = new UnityEvent();
        
        protected bool HasEcho = false;
        protected Entity Entity;

        private float _echo;
        private Coroutine _resetEcho;
        private float _echoLastSetTime;
        
        public float Echo
        {
            get => _echo;
            set { if (_echo != value) { _echo = value; OnPropertyChanged("Echo"); } }
        }
        
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

        protected virtual void Awake()
        {
            Entity = GetComponent<Entity>();
            _echo = _current;
        }

        protected void SetEcho()
        {
            if (_resetEcho == null)
            {
                Echo = Current;
                _resetEcho = StartCoroutine(ResetEcho());
            }
            else if (Time.time < _echoLastSetTime + EchoLifespan)
            {
                StopCoroutine(_resetEcho);
                _resetEcho = StartCoroutine(ResetEcho());
            }
        }


        private IEnumerator ResetEcho()
        {
            _echoLastSetTime = Time.time;
            
            while (Echo >= Current)
            {
                if (Time.time < _echoLastSetTime + EchoLifespan)
                {
                    yield return null;
                }
                else
                {
                    Echo -= Maximum * EchoResetRate * Time.deltaTime;
                    yield return null;
                }
            }

            Echo = 0;
            _resetEcho = null;
        }

        protected void ApplyChange(int delta)
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
    }
}