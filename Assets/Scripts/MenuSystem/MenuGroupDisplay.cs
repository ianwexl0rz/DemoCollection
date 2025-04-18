﻿using UnityEditor;
using UnityEngine;

namespace MenuSystem
{
	public abstract class MenuGroupDisplay : MonoBehaviour
	{
		private Menu menu;
		[SerializeField] private MenuSkin menuSkin = null;
		public MenuGroup menuGroup;
		[SerializeField, HideInInspector] protected MenuItemDisplay[] items;
		protected int selectedIndex;

		public void RegisterMenuItems()
		{
			menu = GetComponentsInParent<Menu>(true)[0];

			if(!menu)
			{
				Debug.Log("MenuGroupDisplay must be the child of a MenuDisplay!");
				return;
			}

			if(!menuSkin)
			{
				Debug.Log("MenuSkin is not defined!");
				return;
			}

			if(menuGroup == null && transform.childCount == 0)
			{
				// Nothing to be done
				return;
			}

			#if UNITY_EDITOR
			Undo.SetCurrentGroupName("Update Menu Group");
			Undo.RegisterFullObjectHierarchyUndo(gameObject, "");
			#endif

			while(transform.childCount > 0)
			{
				DestroyImmediate(transform.GetChild(0).gameObject);
			}

			if(menuGroup != null)
			{
				items = new MenuItemDisplay[menuGroup.configs.Length];

				for(var i = 0; i < menuGroup.configs.Length; i++)
				{
					GameObject go = null;
					switch(menuGroup.configs[i].menuItem)
					{
						case SelectorMenuItem selector:
#if UNITY_EDITOR
							go = ((SelectorMenuItemDisplay)PrefabUtility.InstantiatePrefab(menuSkin.selectorPrefab, transform)).gameObject;
#else
							go = Instantiate(menuSkin.selectorPrefab, transform).gameObject;
#endif
							break;
						case ButtonMenuItem button:
#if UNITY_EDITOR
							go = ((ButtonMenuItemDisplay)PrefabUtility.InstantiatePrefab(menuSkin.buttonPrefab, transform)).gameObject;
#else
							go = Instantiate(menuSkin.buttonPrefab, transform).gameObject;
#endif
							break;
						case ToggleMenuItem toggle:
#if UNITY_EDITOR
							go = ((ToggleMenuItemDisplay)PrefabUtility.InstantiatePrefab(menuSkin.togglePrefab, transform)).gameObject;
#else
							go = Instantiate(menuSkin.togglePrefab, transform).gameObject;
#endif
							break;
						default:
							Debug.LogWarning($"Element at index {i} in {menuGroup.name} is invalid. Skipped!");
							continue;
					}

					var item = go.GetComponent<MenuItemDisplay>();
					item.Initialize(menuGroup.configs[i], menuSkin.itemColors);
					items[i] = item;

#if UNITY_EDITOR
					Undo.RegisterCreatedObjectUndo(go, "");
#endif
				}
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
		}

		public MenuItemDisplay GetMenuItem(int index)
		{
			return items[index];
		}

		public abstract MenuItemDisplay ProcessInput(MenuInput input);
	}
}
