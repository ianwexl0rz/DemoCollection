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

namespace DemoCollection
{
    public class UIController : ObservableMonobehaviour
    {
        private static UIController _instance;

        [SerializeField] private PlayerController playerController = null;
        [SerializeField] private GameSettings gameSettings;

        [SerializeField] private HudBinding hudBinding = null;
        
        [SerializeField] private PauseMenuBinding pauseMenuBinding = null;
        
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
            _pauseView.Visibility = Visibility.Hidden;
            _hudView.Visibility = Visibility.Visible;
            State = ViewState.HUD;
        }

        private void OnPause(object param)
        {
            _pauseView.Visibility = Visibility.Visible;
            _hudView.Visibility = Visibility.Hidden;
            State = ViewState.Paused;
        }

        private void SetActiveViewInternal(ViewState state)
        {
            if (State == state)
                return;
			
            else if (state == ViewState.HUD)
                HudViewCommand.Execute(null);

            else if (state == ViewState.Paused)
                PauseViewCommand.Execute(null);
        }
    }
}
