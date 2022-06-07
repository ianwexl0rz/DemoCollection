using System;
using DemoCollection;
using UnityEngine;

[Serializable]
 public class PauseMode : GameMode
 {
     public static event Action<bool> OnPauseGame = delegate { };

     public override void Init(object context, Action callback = null)
     {
         Time.timeScale = 0;
         MainMode.SetPhysicsPaused(true);
         UIController.SetActiveView(ViewState.Paused);
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
         Time.timeScale = 1;
         UIController.SetActiveView(ViewState.HUD);
         OnPauseGame(false);
     }
 }