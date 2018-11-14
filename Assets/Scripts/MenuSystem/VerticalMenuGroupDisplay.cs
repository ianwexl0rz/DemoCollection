namespace MenuSystem
{
	class VerticalMenuGroupDisplay : MenuGroupDisplay
	{
		public override MenuItemDisplay ProcessInput(MenuDirection input)
		{
			switch(input)
			{
				case MenuDirection.Down:
					selectedIndex = (selectedIndex + 1) % items.Length;
					break;
				case MenuDirection.Up:
					selectedIndex = (selectedIndex + items.Length - 1) % items.Length;
					break;
				case MenuDirection.Right:
					items[selectedIndex].ChangeValue(1);
					break;
				case MenuDirection.Left:
					items[selectedIndex].ChangeValue(-1);
					break;
			}

			return items[selectedIndex];
		}
	}
}
