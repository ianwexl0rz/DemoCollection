using UnityEngine;

namespace MenuSystem
{
	public abstract class MenuItemDisplay : MonoBehaviour
	{
		[SerializeField, HideInInspector] protected MenuItemColors colors;
		[SerializeField] protected MenuItemConfig config;

		public virtual void Initialize(MenuItemConfig config, MenuItemColors colors)
		{
			this.config = config;
			this.colors = colors;

			gameObject.name = config.menuItem.name;
		}

		public abstract void ChangeValue(int delta);

		public abstract void SetSelected(bool value);
	}
}
