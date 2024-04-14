using System;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// The core of the patcher. Executes various steps in sequence.
    /// </summary>
    public static class PatcherSteps {
        private readonly static IPatcherStep[] _defaultSteps = { };

        [MenuItem("Tools/Test Asset Ripper")]
        public static async UniTaskVoid RunTest() {
            var assetRipper = new AssetRipperStep();
            StepResult result = StepResult.Failure;
            try {
                result = await assetRipper.Run();
            } catch (Exception e) {
                Debug.LogException(e);
                result = StepResult.Failure;
            }
            
            Debug.Log($"Result: {result}");
        }
        
        public static async UniTask Run() {
            // todo: determine which step to start on
            var allSteps = _defaultSteps;
            foreach (var step in allSteps) {
                StepResult result;
                try {
                    result = await step.Run();
                } catch (System.Exception) {
                    result = StepResult.Failure;
                }

                if (result.HasFlag(StepResult.Failure)) {
                    break;
                }
                
                if (result.HasFlag(StepResult.RestartEditor)) {
                    // todo: restart editor
                    break;
                }
            }
        }
    }
}