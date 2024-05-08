using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This lets you rename files in the export folder.
    /// </summary>
    public readonly struct RenameFilesInExportStep: IPatcherStep {
        private readonly (string, string)[] _entries;

        public RenameFilesInExportStep(params (string, string)[] entries) {
            _entries = entries;
        }
        
        public UniTask<StepResult> Run() {
            var arSettings = this.GetAssetRipperSettings();
            var exportPath = arSettings.OutputExportAssetsFolderPath;
            
            foreach (var entry in _entries) {
                var oldFilePath = Path.Combine(exportPath, entry.Item1);
                var newFilePath = Path.Combine(exportPath, entry.Item2);
                
                if (!File.Exists(oldFilePath)) {
                    Debug.LogError($"File not found: {oldFilePath}");
                    continue;
                }
                
                File.Move(oldFilePath, newFilePath);
                Debug.Log($"Renamed \"{oldFilePath}\" to \"{newFilePath}\"");
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}