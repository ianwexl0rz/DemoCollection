using DemoCollection.DataBinding;
using UnityEngine;

namespace DemoCollection
{
    public enum ViewState
    {
        HUD,
        Paused
    }
    
    public class UIController : ObservableMonobehaviour
    {
        private static UIController _instance;

        [SerializeField] private PlayerController playerController = null;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private GameObject pauseOverlay;

        [Header("HUD")]
        [SerializeField] private ResourceBar playerHealth = null;
        [SerializeField] private ResourceBar playerStamina = null;

        public void RegisterPlayer(Actor actor)
        {
            playerHealth.RegisterResource(actor.Health);
            playerStamina.RegisterResource(actor.Stamina);
        }
        
        public void UnregisterPlayer(Actor actor)
        {
            playerHealth.UnregisterResource(actor.Health);
            playerStamina.UnregisterResource(actor.Stamina);
        }
        
        private ViewState _state;
        public ViewState State {get => _state; private set => SetProperty(ref _state, value);}

        public void Init()
        {
            playerController.OnPossessActor += RegisterPlayer;
            playerController.OnReleaseActor += UnregisterPlayer;
            
            _instance = this;
            _instance.OnHud();
        }

        public static void SetActiveView(ViewState state)
        {
            _instance.SetActiveViewInternal(state);
        }
        
        private void OnHud()
        {
            pauseOverlay.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            State = ViewState.HUD;
        }

        private void OnPause()
        {
            // Show pause menu
            // TODO: Hide HUD elements that are exclusive to gameplay 
            pauseOverlay.SetActive(true);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            State = ViewState.Paused;
        }

        private void SetActiveViewInternal(ViewState state)
        {
            if (State == state)
                return;

            if (state == ViewState.HUD)
                OnHud();

            else if (state == ViewState.Paused)
                OnPause();
        }
    }
}
