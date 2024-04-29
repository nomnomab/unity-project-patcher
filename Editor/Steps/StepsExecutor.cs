using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public struct StepsExecutor {
        public IPatcherStep[] steps;
        public int index;

        private StepsProgress _progress;
        
        public StepsExecutor(IPatcherStep[] steps) {
            this.steps = steps;
            this.index = 0;

            _progress = new StepsProgress {
                Steps = steps.Select(x => x.GetType().FullName).ToList(),
                CompletedSteps = new()
            };
        }

        public async UniTask<bool> Execute() {
            var stepIndex = 0;
            
            try {
                var stepsProgress = StepsProgress.FromPath(StepsProgress.SavePath);
                if (stepsProgress is not null) {
                    // might have crashed?
                    if (stepsProgress.InProgress) {
                        Debug.LogWarning($"Seems like it might have crashed at step {stepIndex}, aborting...");
                        ClearProgress();
                        return false;
                    }
                    
                    if (stepsProgress.GetCompletion(steps, out stepIndex)) {
                        Debug.Log($"Completed {stepIndex} steps out of {steps.Length}");
                    }
                }
            } catch {
                Debug.LogError("Failed to read steps progress");
                ClearProgress();
                throw;
            }
            
            if (stepIndex >= steps.Length) {
                Debug.Log("All steps are done");
                ClearProgress();
                return true;
            }

            index = stepIndex;
            
            Debug.Log($"Starting on step {stepIndex} -> {steps[stepIndex].GetType().Name}");
            
            EditorUtility.DisplayProgressBar("Patching", "Patching", 0);
            for (int i = index; i < steps.Length; i++) {
                index = i;
                
                // save steps so far
                SaveProgress(true);
                
                // todo: is this even needed?
                // while (EditorApplication.isCompiling) {}
                
                var step = steps[i];
                Debug.Log($"Starting step \"<b>{step.GetType().Name}</b>\"");
                EditorUtility.DisplayProgressBar("Patching", step.GetType().Name, (float)i / steps.Length);
                
                try {
                    var result = await step.Run();
                    _progress.LastResult = result;
                    
                    switch (result) {
                        case StepResult.RestartEditor:
                            index++;
                            SaveProgress(false);
                            
                            //? bypasses the recompilation of scripts so it doesn't trigger the
                            //? patcher twice while closing
                            AssetDatabase.StartAssetEditing();
                            EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                            return false;
                        case StepResult.Failure:
                            throw new Exception($"Step {step.GetType().Name} failed");
                        case StepResult.Recompile:
                            index++;
                            SaveProgress(false);
                            
                            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                            return false;
                        default:
                            Debug.Log($"Step \"<b>{step.GetType().Name}</b>\" completed");
                            break;
                    }
                } catch {
                    Debug.LogError($"Step {step.GetType().Name} failed");
                    ClearProgress();
                    EditorUtility.ClearProgressBar();
                    throw;
                }
                finally {
                    // any cleanup needed
                    EditorUtility.ClearProgressBar();
                }
            }
            
            EditorUtility.ClearProgressBar();
            ClearProgress();
            return true;
        }

        public void SaveProgress(bool inProgress) {
            _progress.CompletedSteps = steps.Take(index)
                .Select(x => x.GetType().FullName)
                .ToList();

            _progress.InProgress = inProgress;

            var json = _progress.ToJson();
            // Debug.Log(json);
            File.WriteAllText(StepsProgress.SavePath, json);
        }

        public void ClearProgress() {
            File.Delete(StepsProgress.SavePath);
            EditorUtility.ClearProgressBar();
        }
    }
}