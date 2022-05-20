#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
#else
using System;
using System.Windows.Controls;
#endif

namespace DemoCollection
{
	/// <summary>
	/// Interaction logic for PauseView.xaml
	/// </summary>
	public partial class PauseView : Grid
	{
		public PauseView()
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
