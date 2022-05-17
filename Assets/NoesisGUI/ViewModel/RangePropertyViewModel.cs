using System;

namespace DemoCollection
{
	[Serializable]
	public class RangePropertyViewModel : ViewModelBase
	{
		private int _current;
		private int _maximum;

		public RangePropertyViewModel(int current, int maximum)
		{
			Current = current;
			Maximum = maximum;
		}

		public int Current
		{
			get => _current;
			set => SetProperty(ref _current, value);
		}

		public int Maximum
		{
			get => _maximum;
			set => SetProperty(ref _maximum, value);
		}
	}
}
