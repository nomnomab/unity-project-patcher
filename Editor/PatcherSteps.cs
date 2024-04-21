using System.Reflection;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// The core of the patcher. Executes various steps in sequence.
    /// </summary>
    public static class PatcherSteps {
        [InitializeOnLoadMethod]
        private static void OnLoad() {
            // locate game patcher
            foreach (var type in TypeCache.GetTypesWithAttribute<UPPatcherAttribute>()) {
                // does it have a Run function?
                var runFunction = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                if (runFunction is null) continue;
                if (runFunction.GetParameters().Length > 0) continue;
                
                Debug.Log($"Found Patcher: {type.Name}");
                
                EditorApplication.delayCall += () => {
                    var progress = StepsProgress.FromPath(StepsProgress.SavePath);
                    if (progress is null) return;

                    runFunction.Invoke(null, null);
                };

                //! only run one patcher
                break;
            }
        }
        
        // private readonly static IPatcherStep[] _defaultSteps = { };
        //
        // [MenuItem("Tools/Test Asset Ripper")]
        // public static async UniTaskVoid RunTest() {
        //     var assetRipper = new AssetRipperStep();
        //     StepResult result = StepResult.Failure;
        //     try {
        //         result = await assetRipper.Run();
        //     } catch (Exception e) {
        //         Debug.LogException(e);
        //         result = StepResult.Failure;
        //     }
        //     
        //     Debug.Log($"Result: {result}");
        // }
        //
        // public static async UniTask Run() {
        //     // todo: determine which step to start on
        //     var allSteps = _defaultSteps;
        //     foreach (var step in allSteps) {
        //         StepResult result;
        //         try {
        //             result = await step.Run();
        //         } catch (System.Exception) {
        //             result = StepResult.Failure;
        //         }
        //
        //         if (result.HasFlag(StepResult.Failure)) {
        //             break;
        //         }
        //         
        //         if (result.HasFlag(StepResult.RestartEditor)) {
        //             // todo: restart editor
        //             break;
        //         }
        //     }
        // }
    }
}