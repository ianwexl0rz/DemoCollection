using UnityEngine;
using InControl;
using UnityEditor;
using System;

namespace MenuSystem
{
	public enum MenuDirection
	{
		Right,
		Up,
		Left,
		Down
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

		private void OnEnable()
		{
			menuGroups = GetComponentsInChildren<MenuGroupDisplay>(true);

			if(InputManager.ActiveDevice.LeftStick.Vector.sqrMagnitude > Mathf.Epsilon)
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
			var inputDevice = InputManager.ActiveDevice;
			var dirInput = inputDevice.LeftStick.Vector;

			if(dirInput.sqrMagnitude < Mathf.Epsilon)
			{
				inputStale = false;
				return;
			}
			else if(inputStale)
			{
				return;
			}

			inputStale = true;

			float angle = Mathf.Atan2(dirInput.y, dirInput.x);
			int quadrant = (int)Mathf.Round(4 * angle / (2 * Mathf.PI) + 4) % 4;

			var previouslySelectedItem = selectedItem;
			selectedItem = selectedGroup.ProcessInput((MenuDirection)quadrant);

			if(selectedItem != previouslySelectedItem)
			{
				previouslySelectedItem.SetSelected(false);
				selectedItem.SetSelected(true);
			}
		}
	}
}
