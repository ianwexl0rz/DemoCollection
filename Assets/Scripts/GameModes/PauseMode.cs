using System;
using UnityEngine;

public partial class GameManager
{
    [Serializable]
    public class PauseMode : GameMode
    {
        [SerializeField] private PauseMenu pauseMenu = null;

        private Action onResume;

        public new class Context : GameMode.Context
        {
            public Action onResume;
        }

        public override void Init(object context, Action callback = null)
        {
            if (!ValidateContext(context, out Context pausedContext)) return;
            onResume = pausedContext.onResume;
            pauseMenu.Show(true);
        }

        public override void Tick(float deltaTime)
        {
            if (player.GetButtonDown(PlayerAction.Pause))
                SetMode(GameModeType.Main, null, onResume);
        }

        public override void LateTick(float deltaTime)
        {
        }

        public override void FixedTick(float deltaTime)
        {
        }

        public override void Clean()
        {
            pauseMenu.Show(false);
        }
    }
}