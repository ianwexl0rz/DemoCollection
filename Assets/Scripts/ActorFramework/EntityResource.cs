using System;
using System.Collections;
using DemoCollection.DataBinding;
using UnityEngine;
using UnityEngine.Events;

namespace ActorFramework
{
    public class EntityResource : ObservableMonobehaviour
    {
        private const float EchoLifespan = 1.0f;
        private const float EchoResetRate = 0.5f;

        public event Action Depleted;

        [SerializeField] private int current = 100;
        [SerializeField] private int maximum = 100;
        [SerializeField] private UnityEvent onDepleted = new();
        
        protected Entity Entity;

        private float _echo;
        private Coroutine _resetEcho;
        private float _echoLastSetTime;
        
        public float Echo
        {
            get => _echo;
            set { if (!Mathf.Approximately(_echo, value)) { _echo = value; OnPropertyChanged(); } }
        }
        
        public int Current
        {
            get => current;
            set { if (current != value) { current = value; OnPropertyChanged(); } }
        }

        public int Maximum
        {
            get => maximum;
            set { if (maximum != value) { maximum = value; OnPropertyChanged(); } }
        }
        
        protected virtual void Awake()
        {
            Entity = GetComponent<Entity>();
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
            var newValue = current + delta;

            // Clamp new health.
            newValue = Mathf.Clamp(newValue, 0, maximum);
        
            // Early out if no change...
            if (newValue == current) return;

            // Update health.
            Current = newValue;
            
            // Destroy if health is zero.
            if (newValue < Mathf.Epsilon)
            {
                Depleted?.Invoke();
                onDepleted.Invoke();
            }
        }
    }
}