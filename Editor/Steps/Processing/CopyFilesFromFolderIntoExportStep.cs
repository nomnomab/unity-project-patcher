using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This copies files from some defined path into the AssetRipper export folder.
    /// </summary>
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
#if UNITY_2020_3_OR_NEWER
                var relativePath = Path.GetRelativePath(_folder, file);
#else
                var relativePath = PathNetCore.GetRelativePath(_folder, file);
#endif
                var targetPath = Path.Combine(arSettings.OutputExportAssetsFolderPath, _outputFolderInExport, relativePath).ToOSPath();
                var folderPath = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(folderPath)) {
                    Directory.CreateDirectory(folderPath);
                }
                File.Copy(file, targetPath, true);
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}