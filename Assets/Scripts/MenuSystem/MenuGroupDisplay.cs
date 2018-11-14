using UnityEditor;
using UnityEngine;

namespace MenuSystem
{
	public abstract class MenuGroupDisplay : MonoBehaviour
	{
		[SerializeField, ReadOnly] private Menu menu;
		[SerializeField] private MenuSkin menuSkin = null;
		public MenuGroup menuGroup;
		[SerializeField, HideInInspector] protected MenuItemDisplay[] items;
		protected int selectedIndex;

		public void RegisterMenuItems()
		{
			#if UNITY_EDITOR
			Undo.SetCurrentGroupName("Update Menu Group");
			Undo.RegisterFullObjectHierarchyUndo(gameObject, "");
			#endif

			menu = GetComponentsInParent<Menu>(true)[0];

			if(!menu)
			{
				Debug.Log("MenuGroupDisplay must be the child of a MenuDisplay!");
				return;
			}

			if(!menuGroup)
			{
				Debug.Log("MenuGroup is not defined!");
				return;
			}

			if(!menuSkin)
			{
				Debug.Log("MenuSkin is not defined!");
				return;
			}

			items = new MenuItemDisplay[menuGroup.configs.Length];

			while(transform.childCount > 0)
			{
				DestroyImmediate(transform.GetChild(0).gameObject);
			}

			for(var i = 0; i < menuGroup.configs.Length; i++)
			{
				var go = Instantiate(menuSkin.multipleChoice, transform);
				var item = go.GetComponent<MenuItemDisplay>();
				item.Initialize(menuGroup.configs[i], menuSkin.itemColors);
				items[i] = item;

				#if UNITY_EDITOR
				Undo.RegisterCreatedObjectUndo(go, "");
				#endif
			}

			EditorUtility.SetDirty(this);

			#if UNITY_EDITOR
			Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
			#endif
		}

		public MenuItemDisplay GetMenuItem(int index)
		{
			return items[index];
		}

		public abstract MenuItemDisplay ProcessInput(MenuDirection input);
	}
}
