using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct CopyProjectSettingsStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            EditorApplication.LockReloadAssemblies();
            PlayerSettings.allowUnsafeCode = true;
            
            var settings = this.GetAssetRipperSettings();
            var arProjectSettingsFolder = Path.Combine(settings.OutputFolderPath, "ExportedProject", "ProjectSettings");
            var projectProjectSettingsFolder = Path.Combine(Application.dataPath, "..", "ProjectSettings");

            try {
                foreach (var name in settings.ProjectSettingFilesToCopy) {
                    var sourcePath = Path.Combine(arProjectSettingsFolder, name);
                    var destinationPath = Path.Combine(projectProjectSettingsFolder, name);
                    if (File.Exists(sourcePath)) {
                        File.Copy(sourcePath, destinationPath, true);
                    }
                }
            } catch {
                Debug.LogError("Failed to copy project settings");
                return UniTask.FromResult(StepResult.Failure);
            }
            
            return UniTask.FromResult(StepResult.RestartEditor);
        }
    }
}