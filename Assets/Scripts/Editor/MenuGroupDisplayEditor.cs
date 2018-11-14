using UnityEditor;
using MenuSystem;
using UnityEngine;

[CustomEditor(typeof(MenuGroupDisplay), true)]
class MenuGroupDisplayEditor : Editor
{
	private MenuGroupDisplay menuGroupDisplay;
	private Editor menuGroupEditor;

	public void OnEnable()
	{
		menuGroupDisplay = (MenuGroupDisplay)target;
		menuGroupEditor = CreateEditor(menuGroupDisplay.menuGroup);
	}

	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();
		serializedObject.Update();
		EditorGUIUtility.wideMode = true;
		EditorGUILayout.PropertyField(serializedObject.FindProperty("menuGroup"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("menuSkin"));
		EditorGUILayout.Space();
		menuGroupEditor.OnInspectorGUI();
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
