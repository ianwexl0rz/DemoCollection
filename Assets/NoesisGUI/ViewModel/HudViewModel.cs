using System;

namespace DemoCollection
{
	public class HudViewModel : ViewModelBase
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

		private RangePropertyViewModel _health;
		public RangePropertyViewModel Health
		{
			get => _health;
			private set => SetProperty(ref _health, value);
		}

#if !UNITY_5_3_OR_NEWER
		public HudViewModel()
		{
			Health = new RangePropertyViewModel(100, 100);
			HasTarget = true;
			TargetX = 960;
			TargetY = 540;
		}
#else
		public static HudViewModel Instance { get; private set; }
		public HudViewModel()
		{
			Instance = this;
			HasTarget = false;
			Health = new RangePropertyViewModel(100, 100);
		}
		
		public void SetLockOnIndicator(bool enabled, UnityEngine.Vector3 lockOnScreenPos)
		{
			HasTarget = enabled && lockOnScreenPos.z > 0;
			TargetX = lockOnScreenPos.x;
			TargetY = UnityEngine.Screen.height - lockOnScreenPos.y;
		}

		public static void RegisterActor(Actor actor)
		{
			actor.Health.OnChanged += Instance.UpdateViewModel;
			Instance.UpdateViewModel(actor.Health);
		}
		
		public static void UnregisterActor(Actor actor)
		{
			actor.Health.OnChanged -= Instance.UpdateViewModel;
		}

		private void UpdateViewModel(ActorFramework.Health health)
		{
			Health.Current = health.Current;
			Health.Maximum = health.Maximum;
		}
#endif

	}
}
