using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;


[Serializable]
public class UnityBoolEvent : UnityEvent<bool> { }

namespace MenuSystem
{
	[CreateAssetMenu(fileName = "New Toggle", menuName = "Menu System/Menu Items/Toggle")]

	public class ToggleMenuItem : MenuItem
	{
		public UnityBoolEvent action;
	}
}
