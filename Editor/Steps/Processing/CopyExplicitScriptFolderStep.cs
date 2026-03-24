using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This copies explicit whole folders from the AssetRipper export's Scripts folder into the project.
    /// This is useful for dlls that have ScriptableObjects, or other asset/component types,
    /// and they should be usable in-editor.
    /// <br/><br/>
    /// These are defined in the UPPatcherSettings assets at <see cref="UPPatcherSettings.ScriptDllFoldersToCopy"/>.
    /// <br/><br/>
    /// Restarts the editor.
    /// </summary>
    public readonly struct CopyExplicitScriptFolderStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            var scriptDllFoldersToCopy = settings.ScriptDllFoldersToCopy;
            
            if (!arSettings.TryGetFolderMapping("Scripts", out var scriptFolder, out var exclude) || exclude) {
                Debug.LogError("Could not find \"Scripts\" folder mapping");
                return UniTask.FromResult(StepResult.Success);
            }
            
            foreach (var scriptDllFolderToCopy in scriptDllFoldersToCopy) {
                var fromFolder = Path.Combine(arSettings.OutputExportAssetsFolderPath, "Scripts", scriptDllFolderToCopy);
                var toFolder = Path.Combine(settings.ProjectGameAssetsPath, scriptFolder, scriptDllFolderToCopy);
                
                // trim fromFolder of generated folders
                var propertiesFolder = Path.Combine(fromFolder, "Properties");
                if (Directory.Exists(propertiesFolder)) {
                    Directory.Delete(propertiesFolder, true);
                }
                
                // delete the asmdef file if it exists
                // var asmdefPath = Path.Combine(fromFolder, $"{scriptDllFolderToCopy}.asmdef");
                // if (File.Exists(asmdefPath)) {
                //     File.Delete(asmdefPath);
                // }
                
                if (Directory.Exists(toFolder)) {
                    try {
                        Directory.Delete(toFolder, true);
                    } catch (Exception e) {
                        Debug.LogError($"Failed to delete {toFolder} with error:\n{e}");
                    }
                }
                
                try {
                    Directory.CreateDirectory(toFolder);
                    foreach (var file in Directory.EnumerateFiles(fromFolder, "*.*", SearchOption.AllDirectories)) {
                        var fileName = Path.GetFileName(file);
                        if (fileName.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes")) {
                            continue;
                        }
                        
                        if (fileName.StartsWith("__")) continue;
                        
#if UNITY_2020_3_OR_NEWER
                        var relativePath = Path.GetRelativePath(fromFolder, file);
#else
                        var relativePath = PathNetCore.GetRelativePath(fromFolder, file);
#endif

                        var targetPath = Path.Combine(toFolder, relativePath).ToOSPath();
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Copy(file, targetPath, true);
                    }
                } catch {
                    Debug.LogError($"Failed to copy from {fromFolder} to {toFolder}");
                    throw;
                }
            }
            
            return UniTask.FromResult(StepResult.RestartEditor);
        }

        public void OnComplete(bool failed) { }
    }
}