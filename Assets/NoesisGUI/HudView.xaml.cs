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
    /// Interaction logic for DemoCollectionMainView.xaml
    /// </summary>
    public partial class HudView : UserControl
    {
        public HudView()
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
