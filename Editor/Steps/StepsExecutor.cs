using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
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
                if (stepsProgress is not null && stepsProgress.GetCompletion(steps, out stepIndex)) {
                    Debug.Log($"Completed {stepIndex} steps out of {steps.Length}");
                }
            } catch {
                Debug.LogError("Failed to read steps progress");
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
                SaveProgress();
                
                while (EditorApplication.isCompiling) {}
                
                var step = steps[i];
                Debug.Log($"Starting step \"<b>{step.GetType().Name}</b>\"");
                EditorUtility.DisplayProgressBar("Patching", step.GetType().Name, (float)i / steps.Length);
                
                try {
                    var result = await step.Run();
                    _progress.LastResult = result;
                    
                    switch (result) {
                        case StepResult.RestartEditor:
                            index++;
                            SaveProgress();
                            // EditorUtility.DisplayDialog("Restarting Unity", "Unity is restarting.", "OK");
                            AssetDatabase.StartAssetEditing();
                            EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                            return false;
                        case StepResult.Failure:
                            throw new Exception($"Step {step.GetType().Name} failed");
                        default:
                            Debug.Log($"Step \"<b>{step.GetType().Name}</b>\" completed");
                            break;
                    }
                } catch {
                    Debug.LogError($"Step {step.GetType().Name} failed");
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

        public void SaveProgress() {
            _progress.CompletedSteps = steps.Take(index)
                .Select(x => x.GetType().FullName)
                .ToList();

            var json = _progress.ToJson();
            // Debug.Log(json);
            File.WriteAllText(StepsProgress.SavePath, json);
        }

        public void ClearProgress() {
            File.Delete(StepsProgress.SavePath);
        }
    }
}