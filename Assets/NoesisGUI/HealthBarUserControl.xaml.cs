#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
#else
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
			typeof(HealthBarUserControl), new PropertyMetadata(100));

		public int Current
		{
			get { return (int)GetValue(CurrentProperty); }
			set { SetValue(CurrentProperty, value); }
		}

		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register("Maximum", typeof(int),
			typeof(HealthBarUserControl), new PropertyMetadata(100));

		public int Maximum
		{
			get { return (int)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}

		public static readonly DependencyProperty EchoProperty =
		DependencyProperty.Register("Echo", typeof(float),
		typeof(HealthBarUserControl), new PropertyMetadata(100f));

		public float Echo
		{
			get { return (float)GetValue(EchoProperty); }
			set { SetValue(EchoProperty, value); }
		}

		public static readonly DependencyProperty BarLengthProperty =
		DependencyProperty.Register("BarLength", typeof(int),
		typeof(HealthBarUserControl), new PropertyMetadata(100));

		public int BarLength
		{
			get { return (int)GetValue(BarLengthProperty); }
			set { SetValue(BarLengthProperty, value); }
		}

		public static readonly DependencyProperty FillProperty =
		DependencyProperty.Register("Fill", typeof(LinearGradientBrush),
		typeof(HealthBarUserControl), new PropertyMetadata(default));

		public int Fill
		{
			get { return (int)GetValue(FillProperty); }
			set { SetValue(FillProperty, value); }
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
