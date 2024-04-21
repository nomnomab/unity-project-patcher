using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public sealed class StepsProgress {
        public static string SavePath => Path.Combine(Application.dataPath, "..", "completed-steps.json");
        
        public List<string> Steps = new();
        public List<string> CompletedSteps = new();
        public StepResult LastResult = StepResult.Success;
        
        public string ToJson() {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
                // TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
                // TypeNameHandling = TypeNameHandling.All
            });
        }
        
        public static StepsProgress? FromPath(string path) {
            if (!File.Exists(path)) {
                return null;
            }
            
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<StepsProgress>(json) ?? null;
        }

        public bool GetCompletion(IPatcherStep[] steps, out int stepIndex) {
            stepIndex = 0;
            
            // make sure steps are the same
            if (Steps.Count != steps.Length) {
                throw new Exception("Steps are not the same length");
            }
                
            for (int i = 0; i < steps.Length; i++) {
                var step = steps[i];
                if (step.GetType().FullName != Steps[i]) {
                    throw new Exception("Steps do not match");
                }
            }

            if (CompletedSteps.Count > steps.Length) {
                throw new Exception("Steps are of an invalid completion length");
            }
                
            for (int i = 0; i < CompletedSteps.Count; i++) {
                var step = steps[i];
                if (step.GetType().FullName == CompletedSteps[i]) {
                    stepIndex++;
                }
            }

            if (LastResult != StepResult.RestartEditor && LastResult != StepResult.Recompile) {
                Debug.Log("Clearing previous progress");
                stepIndex = 0;
                return false;
            }

            var isComplete = stepIndex >= steps.Length;
            return isComplete;
        }
    }
}