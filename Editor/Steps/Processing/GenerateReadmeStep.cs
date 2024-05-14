using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct GenerateReadmeStep: IPatcherStep {
        [MenuItem("Tools/Unity Project Patcher/Other/Generate README.md")]
        private static void Foo() {
            try {
                new GenerateReadmeStep().Run().Forget();
                AssetDatabase.Refresh();
            } catch {
                Debug.LogError("Failed to generate README.md");
                throw;
            }
        }
        
        public UniTask<StepResult> Run() {
            var outputPath = Path.Combine(Application.dataPath, "README.md");
            if (File.Exists(outputPath)) {
                if (!EditorUtility.DisplayDialog("Overwrite README.md?", "Do you want to overwrite the existing README.md?", "Yes", "No")) {
                    return UniTask.FromResult(StepResult.Success);
                }
            }
            
            var settings = this.GetSettings();
            var text = Resources.Load<TextAsset>("UPP/README_TEMPLATE").text;
            
            text = text.Replace("$GAME_NAME$", settings.GameName);
            
            File.WriteAllText(outputPath, text);
            
            Debug.Log($"Generated README.md at {outputPath}");
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}