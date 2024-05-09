using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public struct StepsExecutor {
        public static string CurrentStepName { get; private set; }
        
        public IPatcherStep[] steps;
        public int index;

        private StepsProgress _progress;
        
        public StepsExecutor(IPatcherStep[] steps) {
            this.steps = steps;
            this.index = 0;

            _progress = new StepsProgress {
                Steps = steps.Select(x => x.GetType().FullName).ToList(),
                CompletedSteps = new List<string>(),
            };
        }

        public async UniTask<bool> Execute() {
            var stepIndex = 0;
            
            try {
                var stepsProgress = StepsProgress.FromPath(StepsProgress.SavePath);
                if (stepsProgress != null) {
                    // might have crashed?
                    if (stepsProgress.InProgress) {
                        Debug.LogWarning($"Seems like it might have crashed at step {stepIndex}, aborting...");
                        ClearProgress(true);
                        return false;
                    }
                    
                    if (stepsProgress.GetCompletion(steps, out stepIndex)) {
                        Debug.Log($"Completed {stepIndex} steps out of {steps.Length}");
                    }

                    if (stepsProgress.LastResult == StepResult.RestartEditor) {
                        EditorUtility.DisplayDialog("Focus the Editor", "Please focus the editor!", "Ok");
                    }
                }
            } catch {
                Debug.LogError("Failed to read steps progress");
                ClearProgress(true);
                throw;
            }
            
            if (stepIndex >= steps.Length) {
                Debug.Log("All steps are done");
                ClearProgress(false);
                return true;
            }

            index = stepIndex;

            if (index == 0) {
                SetStartTime();
            }
            
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
                    var startTime = DateTime.Now;
                    CurrentStepName = step.GetType().Name;
                    var result = await step.Run();
                    var endTime = DateTime.Now;
                    var elapsedSeconds = (endTime - startTime).TotalSeconds;
                    AppendStepResult(step, elapsedSeconds);
                    
                    _progress.LastResult = result;
                    
                    Debug.Log($"Step \"<b>{step.GetType().Name}</b>\" took {elapsedSeconds} seconds and returned {result}");
                    
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
                            
#if UNITY_2020_3_OR_NEWER
                            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
#else
                            CompilationPipeline.RequestScriptCompilation();
#endif
                            return false;
                        default:
                            Debug.Log($"Step \"<b>{step.GetType().Name}</b>\" completed");
                            break;
                    }
                } catch {
                    Debug.LogError($"Step {step.GetType().Name} failed");
                    ClearProgress(true);
                    EditorUtility.ClearProgressBar();
                    throw;
                }
                finally {
                    // any cleanup needed
                    EditorUtility.ClearProgressBar();
                }
            }
            
            SetEndTime();
            EditorUtility.ClearProgressBar();
            ClearProgress(false);
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

        public void ClearProgress(bool failed) {
            CurrentStepName = null;
            File.Delete(StepsProgress.SavePath);
            // ClearStepResults();
            EditorUtility.ClearProgressBar();

            if (steps == null || steps.Length == 0) {
                return;
            }
            
            foreach (var step in steps) {
                try {
                    step?.OnComplete(failed);
                } catch {
                    Debug.LogError($"Failed to call OnComplete on \"{step?.GetType().Name}\"");
                    throw;
                }
            }
        }
        
        public void ClearStepResults() {
            if (!File.Exists(StepsResults.SavePath)) {
                return;
            }
            
            File.Delete(StepsResults.SavePath);
        }
        
        public void AppendStepResult(IPatcherStep step, double elapsedSeconds) {
            var stepsResults = GetStepResults();
            stepsResults.Results.Add(new StepsResults.Result(step.GetType().FullName ?? "nil", elapsedSeconds));
            stepsResults.ElapsedSeconds += elapsedSeconds;
            SaveStepResults(stepsResults);
        }
        
        public void SetStartTime() {
            ClearStepResults();
            var stepsResults = GetStepResults();
            stepsResults.StartTime = DateTime.Now;
            stepsResults.EndTime = default;
            stepsResults.ElapsedSeconds = -1;
            SaveStepResults(stepsResults);
        }
        
        public void SetEndTime() {
            var stepsResults = GetStepResults();
            stepsResults.EndTime = DateTime.Now;
            SaveStepResults(stepsResults);
        }
        
        private StepsResults GetStepResults() {
            return StepsResults.FromPath(StepsResults.SavePath);
        }
        
        private void SaveStepResults(StepsResults stepsResults) {
            var json = stepsResults.ToJson();
            // Debug.Log(json);
            File.WriteAllText(StepsResults.SavePath, json);
        }
    }
}