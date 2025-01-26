using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ActorFramework;
using DemoCollection;
using DemoCollection.DataBinding;
using JetBrains.Annotations;
using UnityEngine;
using Noesis;
using Cursor = UnityEngine.Cursor;

namespace DemoCollection
{
    public class UIController : ObservableMonobehaviour
    {
        private static UIController _instance;

        [SerializeField] private PlayerController playerController = null;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private bool noesisEnabled;

        [SerializeField] private HudBinding hudBinding = null;
        
        [SerializeField] private PauseMenuBinding pauseMenuBinding = null;

        [Header("HUD")]
        [SerializeField] private ResourceBar playerHealth = null;
        [SerializeField] private ResourceBar playerStamina = null;

        public static ResourceBar PlayerHealth => _instance.playerHealth;
        public static ResourceBar PlayerStamina => _instance.playerStamina;

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
        
        private FrameworkElement _hudView;
        private FrameworkElement _pauseView;

        public HudBinding HUD => hudBinding;
        public PauseMenuBinding PauseViewModel => pauseMenuBinding;
        
        public DelegateCommand HudViewCommand { get; private set; }
        public DelegateCommand PauseViewCommand { get; private set; }
        
        
        private ViewState _state;
        public ViewState State
        {
            get => _state;
            private set => SetProperty(ref _state, value);
        }
        
        // private object _activeView;
        // public object ActiveView
        // {
        //     get => _activeView;
        //     private set
        //     {
        //         if (_activeView != value)
        //         {
        //             _activeView = value;
        //             OnPropertyChanged("ActiveView");
        //         }
        //     }
        // }

        public void Init()
        {
            _instance = this;
            
            hudBinding = new HudBinding(playerController);
            pauseMenuBinding = new PauseMenuBinding(gameSettings);

            HudViewCommand = new DelegateCommand(OnHud);
            PauseViewCommand = new DelegateCommand(OnPause);

            playerController.OnPossessActor += RegisterPlayer;
            playerController.OnReleaseActor += UnregisterPlayer;
            
            if (!noesisEnabled)
            {
                _instance.OnHud(null);
            }
        }
        
        public static void OnInitialized(FrameworkElement root, out object dataContext)
        {
            _instance._hudView = (FrameworkElement)root.FindName("HudView");
            _instance._pauseView = (FrameworkElement)root.FindName("PauseView");
            _instance.OnHud(null);
            
            dataContext = _instance;
        }

        public static void SetActiveView(ViewState state)
        {
            _instance.SetActiveViewInternal(state);
        }
        
        private void OnHud(object param)
        {
            if (noesisEnabled)
            {
                _pauseView.Visibility = Visibility.Hidden;
                _hudView.Visibility = Visibility.Visible;
            }
            else
            {
                pauseOverlay.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            State = ViewState.HUD;
        }

        private void OnPause(object param)
        {
            if (noesisEnabled)
            {
                _pauseView.Visibility = Visibility.Visible;
                _hudView.Visibility = Visibility.Hidden;
            }
            else
            {
                pauseOverlay.SetActive(true);
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            State = ViewState.Paused;
        }

        private void SetActiveViewInternal(ViewState state)
        {
            if (State == state)
                return;
			
            if (state == ViewState.HUD)
                HudViewCommand.Execute(null);

            else if (state == ViewState.Paused)
                PauseViewCommand.Execute(null);
        }
    }
}
