using UnityEngine;

namespace MenuSystem
{
	[CreateAssetMenu(fileName = "New Menu Item", menuName = "Menu System/Menu Item")]
	public class MenuItem : ScriptableObject
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