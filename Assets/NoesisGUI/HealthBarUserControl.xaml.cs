#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
#else
using System;
using System.Windows;
using System.Windows.Controls;
#endif

namespace DemoCollection
{
	/// <summary>
	/// Interaction logic for HealthBarView.xaml
	/// </summary>
	public partial class HealthBarUserControl : UserControl
	{
		#region Dependency Properties
		public static readonly DependencyProperty CurrentProperty =
			DependencyProperty.Register("Current", typeof(int),
			typeof(HealthBarUserControl), new PropertyMetadata(default));

		public int Current
		{
			get { return (int)GetValue(CurrentProperty); }
			set { SetValue(CurrentProperty, value); }
		}

		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register("Maximum", typeof(int),
			typeof(HealthBarUserControl), new PropertyMetadata(default));

		public int Maximum
		{
			get { return (int)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}
		#endregion

		public HealthBarUserControl()
		{
			InitializeComponent();
		}

#if NOESIS
        private void InitializeComponent()
        {
            NoesisUnity.LoadComponent(this);
        }
#endif
	}
}
