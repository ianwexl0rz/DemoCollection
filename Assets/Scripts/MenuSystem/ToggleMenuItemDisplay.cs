using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MenuSystem
{
	public class ToggleMenuItemDisplay : MenuItemDisplay
	{
		[SerializeField] private TextMeshProUGUI labelText = null;
		[SerializeField] private TextMeshProUGUI valueText = null;

		private ToggleMenuItem toggleMenuItem => (ToggleMenuItem)config.menuItem;
		//private Option[] values => toggleMenuItem.values;
		private bool value => config.value > 0;

		private const string TRUE_STRING = "< On >";
		private const string FALSE_STRING = "< Off >";

		public override void Initialize(MenuItemConfig config, MenuItemColors colors)
		{
			base.Initialize(config, colors);

			labelText.text = toggleMenuItem.name;
			valueText.text = value ? TRUE_STRING : FALSE_STRING;
		}

		public override void ChangeValue(int delta)
		{
			config.value = (config.value + delta) % 2;
			valueText.text = value ? TRUE_STRING : FALSE_STRING;
			toggleMenuItem.action.Invoke(value);
		}

		public override void SetSelected(bool value)
		{
			valueText.color = value ? colors.selected : colors.normal;
		}

		public override void ProcessInput(MenuInput input)
		{
			switch (input)
			{
				case MenuInput.Right:
					ChangeValue(1);
					break;
				case MenuInput.Left:
					ChangeValue(1);
					break;
			}
		}
	}
}
