using System;

namespace DemoCollection
{
	[Serializable]
	public class RangeProperty : ObservableObject
	{
		public int _current = 100;
		public int _maximum = 100;

		public RangeProperty()
		{
		}

		public RangeProperty(int current, int maximum)
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
