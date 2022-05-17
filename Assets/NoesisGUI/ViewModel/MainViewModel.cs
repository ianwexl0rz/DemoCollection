using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoCollection
{
	public class MainViewModel : ViewModelBase
	{
		private object _activeViewModel;
		public object ActiveViewModel
		{
			get => _activeViewModel;
			private set => SetProperty(ref _activeViewModel, value);
		}

		public MainViewModel()
		{
			ActiveViewModel = new HudViewModel();
		}
	}
}
