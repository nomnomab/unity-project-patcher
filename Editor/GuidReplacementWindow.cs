// using UnityEditor;
// using UnityEngine;
//
// namespace Nomnom.UnityProjectPatcher.Editor {
//     sealed internal class GuidReplacementWindow: EditorWindow {
//         [MenuItem("Tools/UPP/Replace GUID")]
//         public static void ShowWindow() {
//             var window = GetWindow<GuidReplacementWindow>("Replace GUID");
//             window.Show();
//         }
//
//         private string _guidString = string.Empty;
//
//         private void OnGUI() {
//             var guid = EditorGUILayout.TextField("GUID", _guidString);
//             if (GUILayout.Button("Replace")) {
//                 
//             }
//         }
//     }
// }