using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct StripNGOGeneratedCodeStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var assetRipperSettings = this.GetAssetRipperSettings();
            var outputPath = assetRipperSettings.OutputFolderPath;
            if (outputPath is null) {
                Debug.LogError("Output path was null");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            var scriptsFolder = Path.Combine(outputPath, "ExportedProject", "Assets", "Scripts", "Assembly-CSharp");
            if (!Directory.Exists(scriptsFolder)) {
                Debug.LogError($"Could not find scripts folder at {scriptsFolder}");
                return UniTask.FromResult(StepResult.Failure);
            }

            try {
                var scriptFiles = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories);

                EditorUtility.DisplayProgressBar("Cleaning decompiled scripts", "Cleaning decompiled scripts", 0.2f);
                ScriptScrubber.ScrubDecompiledScript(scriptFiles, outputCopy: false, Debug.Log);
            } catch {
                Debug.LogError("Failed to clean decompiled scripts");
                throw;
            } finally {
                EditorUtility.ClearProgressBar();
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
    }
}