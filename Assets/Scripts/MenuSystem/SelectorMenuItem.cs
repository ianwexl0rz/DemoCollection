using System;
using UnityEngine;
using UnityEngine.Events;

namespace MenuSystem
{
	[Serializable]
	public class Option
	{
		public string name;
		public UnityEvent action;
	}

	[CreateAssetMenu(fileName = "New Selector", menuName = "Menu System/Menu Items/Selector")]
	public class SelectorMenuItem : MenuItem
	{
		public Option[] values;

		public string[] GetValueNames()
		{
			var names = new string[values.Length];

			for(var i = 0; i < values.Length; i++)
			{
				names[i] = values[i].name;
			}

			return names;
		}
	}
}
