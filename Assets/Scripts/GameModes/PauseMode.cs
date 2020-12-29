using System;
using UnityEngine;

[Serializable]
 public class PauseMode : GameMode
 {
     public static event Action<bool> OnPauseGame = delegate { };

     [SerializeField] private PauseMenu pauseMenu = null;

     public override void Init(object context, Action callback = null)
     {
         MainMode.SetPhysicsPaused(true);
         pauseMenu.Show(true);
         OnPauseGame(true);
     }
 
     public override void Tick(float deltaTime)
     {
         if (player.GetButtonDown(PlayerAction.Pause))
             SetMode<MainMode>();
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
         OnPauseGame(false);
     }
 }