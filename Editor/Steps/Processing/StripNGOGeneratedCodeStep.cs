using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This trims out all of the known NGO generated network code from
    /// a decompiled code-base.
    /// <br/><br/>
    /// This does not run if scripts are stubs.
    /// </summary>
    public readonly struct StripNGOGeneratedCodeStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            if (this.ScriptsAreStubs()) {
                return UniTask.FromResult(StepResult.Success);
            }
            
            var assetRipperSettings = this.GetAssetRipperSettings();
            var outputPath = assetRipperSettings.OutputExportAssetsFolderPath;
            if (outputPath is null) {
                Debug.LogError("Output path was null");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            var scriptsFolder = Path.Combine(outputPath, "Scripts", "Assembly-CSharp");
            if (!Directory.Exists(scriptsFolder)) {
                Debug.LogError($"Could not find scripts folder at {scriptsFolder}");
                return UniTask.FromResult(StepResult.Failure);
            }

            try {
                var scriptFiles = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories);

                EditorUtility.DisplayProgressBar("Cleaning decompiled scripts", "Cleaning decompiled scripts", 0.2f);
                CodeGenUtils.UnityNGOScrubber.ScrubDecompiledScript(scriptFiles, outputCopy: false, Debug.Log);
            } catch {
                Debug.LogError("Failed to clean decompiled scripts");
                throw;
            } finally {
                EditorUtility.ClearProgressBar();
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
        
        public void OnComplete(bool failed) { }
    }
}