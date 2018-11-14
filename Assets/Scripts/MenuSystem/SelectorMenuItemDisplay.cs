using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace MenuSystem
{
	[Serializable]
	public class Option
	{
		public string name;
		public UnityEvent action;
	}

	public class SelectorMenuItemDisplay : MenuItemDisplay
	{
		[SerializeField] private TextMeshProUGUI labelText = null;
		[SerializeField] private TextMeshProUGUI valueText = null;

		private MenuItem MenuItem => config.menuItem;
		private Option[] values => config.menuItem.values;
		private int index => config.value;

		public override void Initialize(MenuItemConfig config, MenuItemColors colors)
		{
			base.Initialize(config, colors);

			labelText.text = MenuItem.name;
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
	}
}
