using UnityEditor;
using MenuSystem;
using UnityEngine;

[CustomEditor(typeof(MenuGroupDisplay), true)]
class MenuGroupDisplayEditor : Editor
{
	private MenuGroupDisplay menuGroupDisplay;
	private MenuGroupEditor menuGroupEditor;
	private MenuGroup cachedMenuGroup;
	private bool needsRefresh;

	public void OnEnable()
	{
		menuGroupDisplay = (MenuGroupDisplay)target;
	}

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		serializedObject.Update();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("menuGroup"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("menuSkin"));
		EditorGUILayout.Space();

		if(cachedMenuGroup != menuGroupDisplay.menuGroup)
		{
			menuGroupEditor = (MenuGroupEditor)CreateEditor(menuGroupDisplay.menuGroup);
			cachedMenuGroup = menuGroupDisplay.menuGroup;
		}

		if(menuGroupEditor != null)
		{
			menuGroupEditor.editable = false;
			menuGroupEditor.list.onReorderCallback = (list) => needsRefresh = true;
			menuGroupEditor.OnInspectorGUI();
		}

		serializedObject.ApplyModifiedProperties();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		needsRefresh |= EditorGUI.EndChangeCheck() || GUILayout.Button(new GUIContent("Refresh"), GUILayout.Width(150));

		if (needsRefresh)
		{
			menuGroupDisplay.RegisterMenuItems();
			needsRefresh = false;
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
}
