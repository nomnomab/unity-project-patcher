// using UnityEditor;
// using UnityEngine;
//
// namespace Nomnom.UnityProjectPatcher.Attributes {
//     public sealed class FolderPathAttribute: PropertyAttribute {
//         public readonly bool UseRelativePath;
//         
//         public FolderPathAttribute(bool useRelativePath = true) {
//             UseRelativePath = useRelativePath;
//         }
//     }
//     
// #if UNITY_EDITOR
//     [UnityEditor.CustomPropertyDrawer(typeof(FolderPathAttribute))]
//     public class FolderPathAttributeDrawer: UnityEditor.PropertyDrawer {
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
//             var folderPathAttribute = attribute as FolderPathAttribute;
//             if (folderPathAttribute == null) {
//                 return;
//             }
//             
//             if (property.propertyType != SerializedPropertyType.String) {
//                 UnityEditor.EditorGUILayout.HelpBox("The FolderPath Attribute can only be attached to a string", UnityEditor.MessageType.Error);
//                 return;
//             }
//             
//             UnityEditor.EditorGUI.BeginChangeCheck();
//             
//             UnityEditor.EditorGUI
//             
//             if (UnityEditor.EditorGUI.EndChangeCheck()) {
//                 property.stringValue = folderPath;
//             }
//         }
//     }
// #endif
// }