using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct GenerateGitIgnoreStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var gitignoreText = Resources.Load<TextAsset>("UPP/Unity.gitignore").text;
            var outputPath = Path.Combine(Application.dataPath, "..", ".gitignore");

            gitignoreText = gitignoreText.Replace("$GAME_NAME$", settings.GameName.Replace(" ", string.Empty));
            
            File.WriteAllText(outputPath, gitignoreText);

            return UniTask.FromResult(StepResult.Success);
        }
    }
}