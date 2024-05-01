using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct CopyFilesFromFolderIntoExportStep: IPatcherStep {
        private readonly string _folder;
        private readonly string _outputFolderInExport;
        
        public CopyFilesFromFolderIntoExportStep(string folder, string outputFolderInExport) {
            _folder = folder;
            _outputFolderInExport = outputFolderInExport;
        }
        
        public UniTask<StepResult> Run() {
            if (!Directory.Exists(_folder)) {
                Debug.LogWarning($"Could not find folder {_folder}");
                return UniTask.FromResult(StepResult.Success);
            }
            
            if (!Directory.Exists(_outputFolderInExport)) {
                Directory.CreateDirectory(_outputFolderInExport);
            }
            
            var arSettings = this.GetAssetRipperSettings();
            var files = Directory.GetFiles(_folder, "*.*", SearchOption.AllDirectories);
            foreach (var file in files) {
                var relativePath = Path.GetRelativePath(_folder, file);
                var targetPath = Path.Combine(arSettings.OutputExportAssetsFolderPath, _outputFolderInExport, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(file, targetPath, true);
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
    }
}