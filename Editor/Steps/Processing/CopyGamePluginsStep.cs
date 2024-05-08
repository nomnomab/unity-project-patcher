using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This simply copies specific dlls defined in <see cref="UPPatcherSettings.DllsToCopy"/>
    /// into the project.
    /// <br/><br/>
    /// Recompiles the editor if a plugin was copied.
    /// </summary>
    public readonly struct CopyGamePluginsStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var dlls = settings.DllsToCopy;

            var gameManagedPath = settings.GameManagedPath;
            var projectPluginsPath = settings.ProjectGamePluginsFullPath;

            EditorUtility.DisplayProgressBar("Copying game dlls", "Copying dlls", 0);

            var copiedOne = false;
            for (var i = 0; i < dlls.Count; i++) {
                var dll = dlls[i];
                try {
                    var fromPath = Path.Combine(gameManagedPath, dll.sourceName);
                    var toPath = Path.Combine(projectPluginsPath, dll.outputPath, Path.GetFileName(dll.sourceName));

                    EditorUtility.DisplayProgressBar("Copying game dlls", $"Copying {fromPath} to {toPath}", (float)i / dlls.Count);

                    if (!File.Exists(fromPath)) {
                        Debug.LogError($"Could not find {fromPath}");
                        continue;
                    }

                    var toFolder = Path.GetDirectoryName(toPath);
                    if (string.IsNullOrEmpty(toFolder)) {
                        Debug.LogError($"Could not find {toPath}");
                        continue;
                    }

                    if (!Directory.Exists(toFolder)) {
                        Directory.CreateDirectory(toFolder);
                    }

                    File.Copy(fromPath, toPath, overwrite: true);
                    copiedOne = true;
                } catch {
                    Debug.LogError($"Failed to copy {dll.sourceName} to {dll.outputPath}");
                    // throw;
                }
                finally {
                    EditorUtility.ClearProgressBar();
                }
            }
            
            if (!copiedOne) {
                Debug.LogWarning("Could not copy any plugin dlls, they might be locked");
                return UniTask.FromResult(StepResult.Success);
            }

            return UniTask.FromResult(StepResult.RestartEditor);
        }

        public void OnComplete(bool failed) { }
    }
}