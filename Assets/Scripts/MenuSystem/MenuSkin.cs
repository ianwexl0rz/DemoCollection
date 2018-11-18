using UnityEngine;

namespace MenuSystem
{
	[CreateAssetMenu(fileName = "New Menu Skin", menuName = "Menu System/Menu Skin")]
	public class MenuSkin : ScriptableObject
	{
		public MenuItemColors itemColors = new MenuItemColors();

		public GameObject multipleChoice = null;
		public GameObject button = null;

		private void OnValidate()
		{
			if(multipleChoice && !multipleChoice.GetComponent<SelectorMenuItemDisplay>())
			{
				multipleChoice = null;
			}

			if(button && !button.GetComponent<ButtonMenuItemDisplay>())
			{
				button = null;
			}
		}
	}
}
