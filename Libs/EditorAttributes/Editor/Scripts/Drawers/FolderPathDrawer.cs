using System.IO;
using UnityEngine;
using UnityEditor;

namespace EditorAttributes.Editor {
	[CustomPropertyDrawer(typeof(FolderPathAttribute))]
	public class FolderPathDrawer : PropertyDrawerBase {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			var folderPathAttribute = attribute as FolderPathAttribute;

			if (property.propertyType != SerializedPropertyType.String) {
				EditorGUILayout.HelpBox("The FolderPath Attribute can only be attached to a string", MessageType.Error);
				return;
			}

			EditorGUI.BeginChangeCheck();

			var buttonWidth = 30f;
			var fieldRect = new Rect(position.x, position.y, position.width - buttonWidth, position.height);

			var folderPath = EditorGUI.TextField(fieldRect, label, property.stringValue);

			var buttonIcon = EditorGUIUtility.IconContent("d_Folder Icon");
			var buttonRect = new Rect(fieldRect.xMax + 2f, position.y, buttonWidth, position.height);

			if (GUI.Button(buttonRect, buttonIcon)) {
				EditorApplication.delayCall += () =>
				{
					folderPath = EditorUtility.OpenFolderPanel("Select folder", "Assets", "");
					if (string.IsNullOrEmpty(folderPath)) {
						return;
					}
					
					if (folderPathAttribute.GetRelativePath && !string.IsNullOrEmpty(folderPath)) {
						var projectRoot = Application.dataPath[..^"Assets".Length];
						folderPath = Path.GetRelativePath(projectRoot, folderPath);
					}

					property.stringValue = folderPath;
					property.serializedObject.ApplyModifiedProperties();
				};
			}

			if (EditorGUI.EndChangeCheck()) {
				if (folderPathAttribute.GetRelativePath && !string.IsNullOrEmpty(folderPath)) {
					var projectRoot = Application.dataPath[..^"Assets".Length];
					folderPath = Path.GetRelativePath(projectRoot, folderPath);
				}

				property.stringValue = folderPath;
				property.serializedObject.ApplyModifiedProperties();
			}
		}
	}
}