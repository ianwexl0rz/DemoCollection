using System;

namespace DemoCollection
{
	public class PauseViewModel : ObservableObject
	{
		private bool _invertX;
		public bool InvertX
		{
			get => _invertX;
			set => SetProperty(ref _invertX, value);
		}
		
		private bool _invertY;
		public bool InvertY
		{
			get => _invertY;
			set => SetProperty(ref _invertY, value);
		}
		
		public DelegateCommand SetLookInvertX { get; private set; }
		public DelegateCommand SetLookInvertY { get; private set; }

		public PauseViewModel()
		{
			SetLookInvertX = new DelegateCommand(OnSetLookInvertX);
			SetLookInvertY = new DelegateCommand(OnSetLookInvertY);
		}

		private void OnSetLookInvertX(object obj)
		{
			InvertX = !InvertX;
			Console.WriteLine($"Set Look Invert X: {obj}");
		}

		private void OnSetLookInvertY(object obj)
		{
			InvertY = !InvertY;
			Console.WriteLine($"Set Look Invert Y: {obj}");
		}
	}
}
