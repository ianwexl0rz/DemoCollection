using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
#if UNITY_5_3_OR_NEWER
using Noesis;
#else
using System.Windows.Controls;
using System.Windows;
#endif

namespace DemoCollection
{
	public enum ViewState
	{
		HUD,
		Paused
	}

	public class HudViewModel : ObservableObject
	{
		private bool _hasTarget;
		public bool HasTarget
		{
			get => _hasTarget;
			private set => SetProperty(ref _hasTarget, value);
		}

		private float _targetX;
		public float TargetX
		{
			get => _targetX;
			private set => SetProperty(ref _targetX, value);
		}

		private float _targetY;
		public float TargetY
		{
			get => _targetY;
			private set => SetProperty(ref _targetY, value);
		}

		private RangeProperty _health;
		public RangeProperty Health
		{
			get => _health;
			set => SetProperty(ref _health, value);
		}
		
		private RangeProperty _stamina;
		public RangeProperty Stamina
		{
			get => _stamina;
			set => SetProperty(ref _stamina, value);
		}

		private RangeProperty _enemyHealth;

		public RangeProperty EnemyHealth
		{
			get => _enemyHealth;
			set => SetProperty(ref _enemyHealth, value);
		}

		private ObservableCollection<TrackableViewModel> _recentlyHit;

		public ObservableCollection<TrackableViewModel> RecentlyHit
		{
			get => _recentlyHit;
			private set => SetProperty(ref _recentlyHit, value);
		}

		public HudViewModel()
		{
			Health = new RangeProperty(100, 100);
			Stamina = new RangeProperty(80, 80);
			EnemyHealth = new RangeProperty(100, 100);
			HasTarget = true;
			TargetX = 960;
			TargetY = 540;

			RecentlyHit = new ObservableCollection<TrackableViewModel>()
			{
				new TrackableViewModel(true, 960/2, 540, new RangeProperty(50, 100)),
				new TrackableViewModel(true, 960 * 3/2, 540, new RangeProperty(50, 100))
			};
		}
	}

	public class TrackableViewModel : ObservableObject
    {
    	private bool _onScreen;
    	public bool OnScreen
    	{
    		get => _onScreen;
    		private set => SetProperty(ref _onScreen, value);
    	}


    	private float _screenPosX;
    	public float ScreenPosX
    	{
    		get => _screenPosX;
    		private set => SetProperty(ref _screenPosX, value);
    	}

        private float _screenPosY;
        public float ScreenPosY
        {
            get => _screenPosY;
            private set => SetProperty(ref _screenPosY, value);
        }

        private RangeProperty _health;
    	public RangeProperty Health
    	{
    		get => _health;
    		private set => SetProperty(ref _health, value);
    	}

        public TrackableViewModel(bool onScreen, float screenPosX, float screenPosY, RangeProperty health)
	    {
            _onScreen = onScreen;
            _screenPosX = screenPosX;
            _screenPosY = screenPosY;
            _health = health;
	    }
    }

	public class ViewModel : ObservableObject
	{
		public HudViewModel HUD { get; private set; }
		public PauseViewModel PauseViewModel { get; private set; }
		public DelegateCommand HudViewCommand { get; private set; }
		public DelegateCommand PauseViewCommand { get; private set; }

		private ViewState _state;
		public ViewState State
		{
			get => _state;
			private set => SetProperty(ref _state, value);
		}

		//private object _activeView;
		//public object ActiveView
		//{
		//	get => _activeView;
		//	private set => SetProperty(ref _activeView, value);
		//}

		private HudView _hudView;
		private PauseView _pauseView;

		public ViewModel()
		{
			HUD = new HudViewModel();
			PauseViewModel = new PauseViewModel();

			//ActiveView = HUD;

			HudViewCommand = new DelegateCommand(OnHud);
			PauseViewCommand = new DelegateCommand(OnPause);
		}

		public void OnInitialized(FrameworkElement root)
		{
			_hudView = (HudView)root.FindName("HudView");
			_pauseView = (PauseView)root.FindName("PauseView");

			OnHud(null);
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

		public void SetActiveView(ViewState viewState)
		{
			if (State == viewState)
				return;
			
			else if (viewState == ViewState.HUD)
				HudViewCommand.Execute(null);

			else if (viewState == ViewState.Paused)
				PauseViewCommand.Execute(null);
		}
	}
}
