using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using MenuSystem;

[CustomEditor(typeof(MenuGroup))]
class MenuGroupEditor : Editor
{
	public ReorderableList list;
	private MenuGroup menuGroup;
	public bool editable = true;

	private void OnEnable()
	{
		menuGroup = (MenuGroup)target;

		list = new ReorderableList(serializedObject,
			serializedObject.FindProperty("configs"),
			true, true, true, true);

		list.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Menu Items");
		list.drawElementCallback = DisplaySetting;
		list.elementHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2);
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		list.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
	}

	private void DisplaySetting(Rect rect, int index, bool isActive, bool isFocused)
	{
		var property = list.serializedProperty.GetArrayElementAtIndex(index);
		EditorGUI.indentLevel = 0;
		rect.y += EditorGUIUtility.standardVerticalSpacing;

		var menuItem = property.FindPropertyRelative("menuItem");
		var value = property.FindPropertyRelative("value");

		var config = menuGroup.configs[index];

		switch (config.menuItem)
		{
			case SelectorMenuItem selectorItem:
				EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), menuItem, GUIContent.none);
				value.floatValue = EditorGUI.Popup(new Rect(rect.x + rect.width - 100, rect.y, 100, EditorGUIUtility.singleLineHeight),
					(int)value.floatValue, selectorItem.GetValueNames());
				break;
			case ButtonMenuItem buttonItem:
				EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), menuItem, GUIContent.none);
				if (GUI.Button(new Rect(rect.x + rect.width - 100, rect.y, 100, EditorGUIUtility.singleLineHeight), buttonItem.name))
					buttonItem.action.Invoke();
				break;
			case ToggleMenuItem toggleMenuItem:
				EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 110, EditorGUIUtility.singleLineHeight), menuItem, GUIContent.none);

				var isOn = config.value > 0;
				if (GUI.Button(new Rect(rect.x + rect.width - 100, rect.y, 100, EditorGUIUtility.singleLineHeight), isOn ? "On" : "Off"))
				{
					config.value = !isOn ? 1 : 0;
					toggleMenuItem.action.Invoke(!isOn);
				}
				break;
			case null:
				if (editable)
					EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), menuItem, GUIContent.none);
				else
					EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "Missing or Invalid Menu Item");
				return;
		}
	}
}
