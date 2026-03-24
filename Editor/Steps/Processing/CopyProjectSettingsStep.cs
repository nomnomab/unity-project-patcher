using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This copies project settings from the export folder into the project.
    /// <br/><br/>
    /// These are defined in <see cref="Nomnom.UnityProjectPatcher.AssetRipper.AssetRipperSettings.ProjectSettingFilesToCopy"/>.
    /// <br/><br/>
    /// Restarts the editor.
    /// </summary>
    public readonly struct CopyProjectSettingsStep: IPatcherStep {
        private readonly bool allowUnsafeCode;
        
        public CopyProjectSettingsStep(bool allowUnsafeCode) {
            this.allowUnsafeCode = allowUnsafeCode;
        }
        
        public UniTask<StepResult> Run() {
            if (allowUnsafeCode) {
                EditorApplication.LockReloadAssemblies();
                PlayerSettings.allowUnsafeCode = true;
            }

            var settings = this.GetAssetRipperSettings();
            var arProjectSettingsFolder = Path.Combine(settings.OutputFolderPath, "ExportedProject", "ProjectSettings");
            var projectProjectSettingsFolder = Path.Combine(Application.dataPath, "..", "ProjectSettings");

            try {
                foreach (var name in settings.ProjectSettingFilesToCopy) {
                    var sourcePath = Path.Combine(arProjectSettingsFolder, name).ToOSPath();
                    var destinationPath = Path.Combine(projectProjectSettingsFolder, name).ToOSPath();
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
        
        public void OnComplete(bool failed) { }
    }
}