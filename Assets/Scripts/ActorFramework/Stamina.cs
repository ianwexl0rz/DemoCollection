using System;
using UnityEngine;

namespace ActorFramework
{
    public class Stamina : EntityResource
    {
        [SerializeField] private int regenRate;
        [SerializeField] private int runningBurnRate;
        [SerializeField] private float minRestTime;

        private ActorKinematicMotor _motor;
        private float _accumulator;
        private float _staminaLastSpentTime;

        protected override void Awake()
        {
            base.Awake();
            _motor = Entity.GetComponent<ActorKinematicMotor>();
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
            if (!_motor) return;
            
            if (_motor.IsRunning)
            {
                _staminaLastSpentTime = Time.time;
                _accumulator += (float)runningBurnRate * deltaTime;
                ApplyChange(-Mathf.FloorToInt(_accumulator));
                _accumulator = _accumulator % 1;
            }
            else if (Current < Maximum && Time.time >= _staminaLastSpentTime + minRestTime)
            {
                _accumulator += (float)regenRate * deltaTime;
                ApplyChange(Mathf.FloorToInt(_accumulator));
                _accumulator = _accumulator % 1;
            }
            else if (_accumulator != 0)
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