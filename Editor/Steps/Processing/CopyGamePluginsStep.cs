using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct CopyGamePluginsStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var dlls = settings.DllsToCopy;

            var gameManagedPath = settings.GameManagedPath;
            var projectPluginsPath = settings.ProjectGamePluginsPath;

            EditorUtility.DisplayProgressBar("Copying game dlls", "Copying dlls", 0);

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
                } catch {
                    Debug.LogError($"Failed to copy {dll.sourceName} to {dll.outputPath}");
                    throw;
                }
                finally {
                    EditorUtility.ClearProgressBar();
                }
            }

            return UniTask.FromResult(StepResult.Success);
        }
    }
}