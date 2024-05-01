using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    [Serializable]
    public class StepsResults {
        public static string SavePath => Path.Combine(Application.dataPath, "..", "steps-results.json");

        public DateTime StartTime;
        public DateTime EndTime;
        public double ElapsedSeconds;
        public List<Result> Results = new List<Result>();
        
        public string ToJson() {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
                // TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
                // TypeNameHandling = TypeNameHandling.All
            });
        }
        
        [Serializable]
        public struct Result {
            public string fullName;
            public double elapsedSeconds;
            
            public Result(string fullName, double elapsedSeconds) {
                this.fullName = fullName;
                this.elapsedSeconds = elapsedSeconds;
            }
        }

        public static StepsResults FromPath(string savePath) {
            if (!File.Exists(savePath)) {
                return new StepsResults();
            }
            
            var json = File.ReadAllText(savePath);
            return JsonConvert.DeserializeObject<StepsResults>(json) ?? new StepsResults();
        }
    }
}