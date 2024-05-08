using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Imports the TextMeshPro essentials so the user doesn't have to use the pop-up.
    /// </summary>
    public readonly struct ImportTextMeshProStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            EditorUtility.DisplayProgressBar("Installing packages", "Installing TMP Essential Resources", 1f);
            try {
                AssetDatabase.ImportPackage("Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage", false);
            } catch {
                Debug.LogError("Failed to install TMP Essential Resources package");
            }
            EditorUtility.ClearProgressBar();
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}