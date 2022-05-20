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
    public partial class MainWindow : Grid
    {
        public MainWindow()
        {
            Initialized += OnInitialized;
            InitializeComponent();
        }

        private void OnInitialized(object sender, EventArgs e)
        {
#if UNITY_5_3_OR_NEWER
            UIController.OnInitialized(this, out var dataContext);
#else
            var dataContext = new ViewModel();
            dataContext.OnInitialized(this);
#endif

            DataContext = dataContext;
        }

#if NOESIS
        private void InitializeComponent()
        {
            NoesisUnity.LoadComponent(this);
        }
#endif
    }
}
