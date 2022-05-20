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
    public class UIController : MonoBehaviour, INotifyPropertyChanged
    {
        private static UIController _instance;

        [SerializeField] private HUDController hudController = null;
        [SerializeField] private PauseMenuController pauseMenuController = null;
        
        private FrameworkElement _hudView;
        private FrameworkElement _pauseView;

        public HUDController HUD => hudController;

        public PauseMenuController PauseViewModel => pauseMenuController;
        
        public DelegateCommand HudViewCommand { get; private set; }
        public DelegateCommand PauseViewCommand { get; private set; }
        
        private ViewState _state;
        public ViewState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        public void Init()
        {
            _instance = this;
            HUD.Init();

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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
