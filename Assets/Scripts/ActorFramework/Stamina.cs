using System;
using UnityEngine;

namespace ActorFramework
{
    public class Stamina : EntityResource
    {
        [SerializeField] private int regenRate;
        [SerializeField] private int runningBurnRate;
        [SerializeField] private float minRestTime;
        
        private IActorMotor _motor;
        private float _accumulator;
        private float _staminaLastSpentTime;

        protected override void Awake()
        {
            base.Awake();
            _motor = Entity.GetComponent<IActorMotor>();
        }

        private void OnEnable()
        {
            Entity.LateTick += UpdateStamina;
        }

        private void OnDisable()
        {
            Entity.LateTick -= UpdateStamina;
        }

        private void UpdateStamina(float deltaTime)
        {
            if (_motor is { IsRunning: true })
            {
                _staminaLastSpentTime = Time.time;
                _accumulator += runningBurnRate * deltaTime;
                ApplyChange(-Mathf.FloorToInt(_accumulator));
                _accumulator %= 1.0f;
            }
            else if (Current < Maximum && Time.time >= _staminaLastSpentTime + minRestTime)
            {
                _accumulator += regenRate * deltaTime;
                ApplyChange(Mathf.FloorToInt(_accumulator));
                _accumulator %= 1.0f;
            }
            else
            {
                _accumulator = 0;
            }
        }

        public void SpendStamina(int amount)
        {
            _staminaLastSpentTime = Time.time;
            SetEcho();
            ApplyChange(-amount);
        }
    }
}