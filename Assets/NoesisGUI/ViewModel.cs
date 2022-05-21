using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

	public class ViewModel : ObservableObject
	{
		public HUD HUD { get; private set; }
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
			HUD = new HUD();
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
