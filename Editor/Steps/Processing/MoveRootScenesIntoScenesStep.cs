using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct MoveRootScenesIntoScenesStep: IPatcherStep {
        // [MenuItem("Tools/UPP/Move Root Scenes Into Scenes (Will be removed)")]
        // private static void Foo() {
        //     var step = new MoveRootScenesIntoScenesStep();
        //     try {
        //         step.Run().Forget();
        //     } catch (Exception e) {
        //         Debug.LogError(e);
        //         EditorUtility.ClearProgressBar();
        //     }
        // }
        
        public UniTask<StepResult> Run() {
            var arSettings = this.GetAssetRipperSettings();
            var sceneFiles = Directory.GetFiles(arSettings.OutputExportAssetsFolderPath, "*.unity", SearchOption.TopDirectoryOnly);
            foreach (var file in sceneFiles) {
                var metaFile = file + ".meta";
                var movedFileInScenesFolder = Path.Combine(arSettings.OutputExportAssetsFolderPath, "Scenes", Path.GetFileName(file));
                var movedMetaFileInScenesFolder = movedFileInScenesFolder + ".meta";

                try {
                    File.Move(file, movedFileInScenesFolder);
                } catch {
                    Debug.LogWarning($"Could not move \"{file}\" to \"{movedFileInScenesFolder}\"");
                }
                
                try {
                    File.Move(metaFile, movedMetaFileInScenesFolder);
                } catch {
                    Debug.LogWarning($"Could not move \"{metaFile}\" to \"{movedMetaFileInScenesFolder}\"");
                }

                var folder = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                var movedFolderInScenesFolder = Path.Combine(arSettings.OutputExportAssetsFolderPath, "Scenes", Path.GetFileName(folder));
                try {
                    if (Directory.Exists(folder)) {
                        Directory.Move(folder, movedFolderInScenesFolder);
                    }
                } catch {
                    Debug.LogWarning($"Could not move \"{folder}\" to \"{movedFolderInScenesFolder}\"");
                }
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}