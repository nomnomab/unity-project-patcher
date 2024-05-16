using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct MoveFilesInExportStep: IPatcherStep {
        // [MenuItem("Tools/Unity Project Patcher/Other/Move Files In Export")]
        // private static void Foo() {
        //     try {
        //         new MoveFilesInExportStep(
        //             (Path.Combine("Scripts", "Assembly-CSharp", "DunGen", "Editor", "*"), Path.Combine("Scripts", "Assembly-CSharp", "DunGen"))
        //         ).Run().Forget();
        //     } catch {
        //         Debug.LogError("Failed to move files in export folder.");
        //         EditorUtility.ClearProgressBar();
        //         throw;
        //     }
        // }
        
        private readonly (string from, string to)[] _filesToMove;
        
        public MoveFilesInExportStep(params (string from, string to)[] filesToMove) {
            _filesToMove = filesToMove;
        }
        
        public UniTask<StepResult> Run() {
            var arSettings = this.GetAssetRipperSettings();
            var exportPath = arSettings.OutputExportAssetsFolderPath;

            foreach (var file in _filesToMove) {
                var fromPath = Path.Combine(exportPath, file.from);
                var toPath = Path.Combine(exportPath, file.to);

                try {
                    var fromFolder = Path.GetDirectoryName(fromPath);
                    var toFolder = Path.GetDirectoryName(toPath);
                    
                    var fromFileName = Path.GetFileName(fromPath);
                    var toFileName = Path.GetFileName(toPath);

                    if (!Directory.Exists(fromFolder)) {
                        Directory.CreateDirectory(fromFolder);
                    }

                    if (!Directory.Exists(toFolder)) {
                        Directory.CreateDirectory(toFolder);
                    }

                    if (fromFileName == "*") {
                        Debug.Log($"Moving all contents of \"{fromFolder}\" to \"{toFolder}\"");
                        // copy all to the output
                        foreach (var fromFile in Directory.GetFiles(fromFolder, "*", SearchOption.AllDirectories)) {
                            var fromPathTrimmed = fromFile.Substring(fromFolder.Length + 1);
                            var newPath = Path.Combine(toPath, fromPathTrimmed);
                            Debug.Log($"Moving \"{fromFile}\" to \"{newPath}\"");
                            var folder = Path.GetDirectoryName(newPath);
                            if (!Directory.Exists(folder)) {
                                Directory.CreateDirectory(folder);
                            }
                            File.Move(fromFile, newPath);
                            Debug.Log($"Moved \"{fromFile}\" to \"{newPath}\"");
                        }
                    } else {
                        File.Move(fromPath, toPath);
                        Debug.Log($"Moved \"{fromPath}\" to \"{toPath}\"");
                    }
                } catch(Exception e) {
                    Debug.LogWarning($"Could not move \"{fromPath}\" to \"{toPath}\": {e}");
                }
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}