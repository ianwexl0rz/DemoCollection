using UnityEngine;

namespace MenuSystem
{
	[CreateAssetMenu(fileName = "New Menu Skin", menuName = "Menu System/Menu Skin")]
	public class MenuSkin : ScriptableObject
	{
		public MenuItemColors itemColors = new MenuItemColors();

		public ButtonMenuItemDisplay buttonPrefab = null;
		public ToggleMenuItemDisplay togglePrefab = null;
		public SelectorMenuItemDisplay selectorPrefab = null;

	}
}
