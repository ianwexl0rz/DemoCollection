using UnityEngine;

namespace ActorFramework
{
    public struct CharacterInputs
    {
        public Vector3 Move;
        public Vector3 Look;
        public bool Run;
        public bool BeginJump;
        public bool BeginRoll;
    }
    
    public interface IActorMotor
    {
        void SetInputs(ref CharacterInputs inputs);
        
        public bool IsRunning { get; }
    }
}