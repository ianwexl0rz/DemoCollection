using System;

namespace DemoCollection
{
	[Serializable]
	public class RangeProperty : ObservableObject
	{
		public float _echo = 100;
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
		
		public float Echo
		{
			get => _echo;
			set => SetProperty(ref _echo, value);
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
