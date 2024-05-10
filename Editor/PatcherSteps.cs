using System.Reflection;
using Cysharp.Threading.Tasks;
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
            var progress = StepsProgress.FromPath(StepsProgress.SavePath);
            if (progress is null) return;

            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static void Update() {
            EditorApplication.update -= Update;
            Run();
        }

        private static void StartDelay(MethodInfo func) {
            EditorApplication.delayCall += () => {
                RunFunction(func);
            };
        }

        public static void Run() {
            // locate game patcher
            var gameWrapperType = PatcherUtility.GetGameWrapperType();
            if (gameWrapperType is null) {
                Debug.LogError("Could not find GameWrapper type");
                return;
            }
            
            var func = PatcherUtility.GetGameWrapperGetStepsFunction(gameWrapperType);
            if (func is null) {
                Debug.LogError("Could not find GameWrapper.GetSteps function");
                return;
            }

            // StartDelay(func);
            RunFunction(func);
        }
        
        private static void RunFunction(MethodInfo func) {
            var pipeline = new StepPipeline();
            func.Invoke(null, new object[] { pipeline });
            if (!pipeline.Validate()) {
                Debug.LogError("Pipeline validation failed");
                return;
            }
            
            var executor = new StepsExecutor(pipeline.Steps.ToArray());
            executor.Execute();
        }

        public static StepPipeline GetPipeline() {
            var pipeline = new StepPipeline();
            
            var gameWrapperType = PatcherUtility.GetGameWrapperType();
            if (gameWrapperType is null) {
                return pipeline;
            }
            
            var func = PatcherUtility.GetGameWrapperGetStepsFunction(gameWrapperType);
            if (func is null) {
                return pipeline;
            }
            
            func.Invoke(null, new object[] { pipeline });
            if (!pipeline.Validate()) return null;
            
            return pipeline;
        }
    }
}