using UnityEngine;
using TMPro;

namespace MenuSystem
{
	public class ButtonMenuItemDisplay : MenuItemDisplay
	{
		[SerializeField] private TextMeshProUGUI labelText = null;

		private ButtonMenuItem buttonMenuItem => (ButtonMenuItem)config.menuItem;

		public override void Initialize(MenuItemConfig config, MenuItemColors colors)
		{
			base.Initialize(config, colors);

			labelText.text = buttonMenuItem.name;
		}

		public override void ChangeValue(int delta)
		{
		}

		public override void SetSelected(bool value)
		{
			labelText.color = value ? colors.selected : colors.normal;
		}

		public override void ProcessInput(MenuInput input)
		{
			switch(input)
			{
				case MenuInput.Submit:
					buttonMenuItem.action.Invoke();
					break;
			}
		}
	}
}
