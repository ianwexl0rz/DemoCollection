using System;
using UnityEngine;

namespace MenuSystem
{
	[Serializable]
	public class MenuItemConfig
	{
		public MenuItem menuItem;
		public float value;
	}

	[CreateAssetMenu(fileName = "New Menu Group", menuName = "Menu System/Menu Group")]
	public class MenuGroup : ScriptableObject
	{
		public MenuItemConfig[] configs;
	}
}