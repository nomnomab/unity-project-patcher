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
    }
}