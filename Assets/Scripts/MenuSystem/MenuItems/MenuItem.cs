using UnityEngine;

namespace MenuSystem
{
	public class MenuItem : ScriptableObject
	{
		public void PrintString(string text)
		{
			Debug.Log(text);
		}
	}
}