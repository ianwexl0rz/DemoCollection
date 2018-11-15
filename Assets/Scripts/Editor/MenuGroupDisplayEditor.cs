using UnityEditor;
using MenuSystem;
using UnityEngine;

[CustomEditor(typeof(MenuGroupDisplay), true)]
class MenuGroupDisplayEditor : Editor
{
	private MenuGroupDisplay menuGroupDisplay;
	private Editor menuGroupEditor;
	private MenuGroup cachedMenuGroup;

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
			menuGroupEditor = CreateEditor(menuGroupDisplay.menuGroup);
			cachedMenuGroup = menuGroupDisplay.menuGroup;
		}

		if(menuGroupEditor != null)
		{
			menuGroupEditor.OnInspectorGUI();
		}

		serializedObject.ApplyModifiedProperties();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		if(EditorGUI.EndChangeCheck() ||
		   GUILayout.Button(new GUIContent("Refresh"),
			GUILayout.Width(150)))
		{
			menuGroupDisplay.RegisterMenuItems();
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
}
