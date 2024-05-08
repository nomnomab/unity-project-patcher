using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct CacheProjectCatalogueStep: IPatcherStep {
        public static string ExportPath => Path.Combine(Application.dataPath, "..", "project-assets-cache.json");
        
        public UniTask<StepResult> Run() {
            var catalogue = AssetScrubber.ScrubProject();
            var json = catalogue.ToJson();
            File.WriteAllText(ExportPath, json);
            
            return UniTask.FromResult(StepResult.Recompile);
        }

        public void OnComplete(bool failed) {
            if (File.Exists(ExportPath)) {
                File.Delete(ExportPath);
            }
        }
    }
}