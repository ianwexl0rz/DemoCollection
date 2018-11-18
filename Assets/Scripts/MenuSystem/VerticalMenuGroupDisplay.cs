namespace MenuSystem
{
	class VerticalMenuGroupDisplay : MenuGroupDisplay
	{
		public override MenuItemDisplay ProcessInput(MenuInput input)
		{
			switch(input)
			{
				case MenuInput.Down:
					selectedIndex = (selectedIndex + 1) % items.Length;
					break;
				case MenuInput.Up:
					selectedIndex = (selectedIndex + items.Length - 1) % items.Length;
					break;
				default:
					items[selectedIndex].ProcessInput(input);
					break;
			}

			return items[selectedIndex];
		}
	}
}
