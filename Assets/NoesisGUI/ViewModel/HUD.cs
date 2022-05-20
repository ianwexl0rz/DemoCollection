using System;

namespace DemoCollection
{
	public class HUD : ObservableObject
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
		
		public HUD()
		{
			Health = new RangeProperty(100, 100);
			HasTarget = true;
			TargetX = 960;
			TargetY = 540;
		}
	}
}
