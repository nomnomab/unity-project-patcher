using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.CodeGenUtils;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This runs an internal code generator that strips out unused methods
    /// from a provided delegate.
    /// <br/><br/>
    /// This does not run if scripts are stubs.
    /// </summary>
    public readonly struct StripMethodsStep: IPatcherStep {
        private readonly Func<MethodRemoval.NodeInfo, bool> _canRemoveFunction;
        
        public StripMethodsStep(Func<MethodRemoval.NodeInfo, bool> canRemoveFunction) {
            _canRemoveFunction = canRemoveFunction;
        }

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

                EditorUtility.DisplayProgressBar("Cleaning scripts", "Cleaning scripts", 0.2f);
                MethodRemoval.Scrub(scriptFiles, _canRemoveFunction, Debug.Log);
            } catch {
                Debug.LogError("Failed to clean scripts");
                throw;
            } finally {
                EditorUtility.ClearProgressBar();
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}