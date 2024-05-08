using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This simply generates a generic gitignore.
    /// </summary>
    public readonly struct GenerateGitIgnoreStep: IPatcherStep {
        private readonly string _appendString;
        
        public GenerateGitIgnoreStep(string appendString = "") {
            _appendString = appendString;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var gitignoreText = Resources.Load<TextAsset>("UPP/Unity.gitignore").text + $"\n{_appendString}";
            var outputPath = Path.Combine(Application.dataPath, "..", ".gitignore");

            gitignoreText = gitignoreText.Replace("$GAME_NAME$", settings.GameName.Replace(" ", string.Empty));
            
            File.WriteAllText(outputPath, gitignoreText);

            return UniTask.FromResult(StepResult.Success);
        }
        
        public void OnComplete(bool failed) { }
    }
}