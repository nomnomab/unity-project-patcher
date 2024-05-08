using System.Reflection;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// The core of the patcher. Executes various steps in sequence.
    /// </summary>
    public static class PatcherSteps {
        [InitializeOnLoadMethod]
        private static void OnLoad() {
            // locate game patcher
            var gameWrapperType = PatcherUtility.GetGameWrapperType();
            if (gameWrapperType is null) return;
            
            var runFunction = PatcherUtility.GetGameWrapperRunFunction(gameWrapperType);
            if (runFunction is null) return;

            StartDelay(runFunction);

            // if (!InternalEditorUtility.isApplicationActive) {
            //     EditorApplication.update -= Update;
            //     EditorApplication.update += Update;
            //     RunFunction(runFunction);
            // } else {
            //     StartDelay(runFunction);
            // }
        }

        // private static void Update() {
        //     if (!InternalEditorUtility.isApplicationActive) {
        //         EditorApplication.delayCall?.Invoke();
        //     }
        // }

        private static void StartDelay(MethodInfo runFunction) {
            EditorApplication.delayCall += () => {
                RunFunction(runFunction);
            };
        }

        private static void RunFunction(MethodInfo runFunction) {
            var progress = StepsProgress.FromPath(StepsProgress.SavePath);
            if (progress is null) return;

            runFunction.Invoke(null, null);
        }
    }
}