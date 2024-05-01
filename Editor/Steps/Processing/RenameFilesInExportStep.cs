using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct RenameFilesInExportStep: IPatcherStep {
        private readonly (string, string)[] _entries;
        
        [MenuItem("Tools/UPP/Test Rename Files In Export")]
        public static void Test() {
            new RenameFilesInExportStep(
                ("Shader\\TextMeshPro_Distance Field.shader", "Shader\\TMP_SDF.shader"),
                ("Shader\\TextMeshPro_Distance Field.shader.meta", "Shader\\TMP_SDF.shader.meta"),
                ("Shader\\TextMeshPro_Mobile_Distance Field.shader", "Shader\\TMP_SDF-Mobile.shader"),
                ("Shader\\TextMeshPro_Mobile_Distance Field.shader.meta", "Shader\\TMP_SDF-Mobile.shader.meta"),
                ("Shader\\TextMeshPro_Sprite.shader", "Shader\\TMP_Sprite.shader"),
                ("Shader\\TextMeshPro_Sprite.shader.meta", "Shader\\TMP_Sprite.shader.meta")
            ).Run().Forget();
        }
        
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
    }
}