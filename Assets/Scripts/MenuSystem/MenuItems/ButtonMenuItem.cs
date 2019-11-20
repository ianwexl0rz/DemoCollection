using UnityEngine;
using UnityEngine.Events;

namespace MenuSystem
{
	[CreateAssetMenu(fileName = "New Button", menuName = "Menu System/Menu Items/Button")]
	public class ButtonMenuItem : MenuItem
	{
		public UnityEvent action;
	}
}
