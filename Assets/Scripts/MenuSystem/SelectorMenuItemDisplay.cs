using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace MenuSystem
{
	public class SelectorMenuItemDisplay : MenuItemDisplay
	{
		[SerializeField] private TextMeshProUGUI labelText = null;
		[SerializeField] private TextMeshProUGUI valueText = null;

		private SelectorMenuItem selectorMenuItem => (SelectorMenuItem)config.menuItem;
		private Option[] values => selectorMenuItem.values;
		private int index => (int)config.value;

		public override void Initialize(MenuItemConfig config, MenuItemColors colors)
		{
			base.Initialize(config, colors);

			labelText.text = selectorMenuItem.name;
			valueText.text = "< " + values[index].name + " >";
		}

		public override void ChangeValue(int delta)
		{
			config.value = (index + delta + values.Length) % values.Length;
			valueText.text = "< " + values[index].name + " >";
			values[index].action.Invoke();
		}

		public override void SetSelected(bool value)
		{
			valueText.color = value ? colors.selected : colors.normal;
		}

		public override void ProcessInput(MenuInput input)
		{
			switch(input)
			{
				case MenuInput.Right:
					ChangeValue(1);
					break;
				case MenuInput.Left:
					ChangeValue(-1);
					break;
			}
		}
	}
}
