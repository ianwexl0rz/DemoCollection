using UnityEngine;
using System;
using Rewired;

namespace MenuSystem
{
	public enum MenuInput
	{
		Right,
		Up,
		Left,
		Down,
		Submit
	}

	[Serializable]
	public class MenuItemColors
	{
		public Color normal = Color.white;
		public Color selected = Color.cyan;
	}

	public class Menu : MonoBehaviour
	{
		[SerializeField, HideInInspector] private MenuGroupDisplay[] menuGroups;
		private MenuGroupDisplay selectedGroup;
		private MenuItemDisplay selectedItem;
		private bool inputStale;
		private Player player;

		private void OnEnable()
		{
			player = ReInput.players.GetPlayer(0);
			menuGroups = GetComponentsInChildren<MenuGroupDisplay>(true);

			foreach(var group in menuGroups)
			{
				group.RegisterMenuItems();
			}

			if(player.GetAxis2D(PlayerAction.MenuHorizontal,PlayerAction.MenuVertical).sqrMagnitude > 0)
			{
				inputStale = true;
			}

			selectedGroup = menuGroups[0];

			if(!selectedItem)
			{
				selectedItem = selectedGroup.GetMenuItem(0);
				selectedItem.SetSelected(true);
			}
		}

		private void Update()
		{
			if(player.GetButtonDown(PlayerAction.Confirm))
			{
				selectedGroup.ProcessInput(MenuInput.Submit);
				return;
			}

			var dirInput = player.GetAxis2D(PlayerAction.MenuHorizontal, PlayerAction.MenuVertical);
			if(dirInput.Equals(Vector2.zero))
			{
				inputStale = false;
				return;
			}
			
			if(inputStale) return;

			inputStale = true;

			var angle = Mathf.Atan2(dirInput.y, dirInput.x);
			var quadrant = (int)Mathf.Round(4 * angle / (2 * Mathf.PI) + 4) % 4;
			var previouslySelectedItem = selectedItem;
			selectedItem = selectedGroup.ProcessInput((MenuInput)quadrant);

			if(selectedItem != previouslySelectedItem)
			{
				previouslySelectedItem.SetSelected(false);
				selectedItem.SetSelected(true);
			}
		}
	}
}
