using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.AssetRipper;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct CopyAssetRipperExportToProjectStep: IPatcherStep {
        private static readonly string[] _ignoreFiles = {
            "UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs",
            "AssemblyInfo.cs"
        };

        [MenuItem("Tools/UPP/Test Copy")]
        private static void Copy() {
            var step = new CopyAssetRipperExportToProjectStep();
            step.Run().Forget();
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            var gameFolderPath = settings.ProjectGameAssetsFullPath;
            Directory.CreateDirectory(gameFolderPath);
            
            // copy the export into the proper folders
            var arAssets = AssetScrubber.ScrubDiskFolder(arSettings.OutputExportAssetsFolderPath, arSettings.FoldersToExcludeFromRead);
            var projectAssets = AssetScrubber.ScrubProject();

            var projectGameAssetsPath = settings.ProjectGameAssetsPath;
            
            var allowedEntries = GetAllowedEntries(arAssets, projectAssets, arSettings).ToArray();
            AssetDatabase.StartAssetEditing();
            for (var i = 0; i < allowedEntries.Length; i++) {
                var asset = allowedEntries[i];

                EditorUtility.DisplayProgressBar("Copying assets", $"Copying {asset.RelativePathToRoot}", i / (float)allowedEntries.Length);
                
                try {
                    var projectPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, asset, settings, arSettings, false);
                    if (projectPath is null) {
                        // failed to find project path
                        // throw new System.NotImplementedException();
                        Debug.LogWarning($" - Could not find project path for \"{asset.RelativePathToRoot}\"");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
                    var exportPath = Path.Combine(arAssets.RootAssetsPath, asset.RelativePathToRoot);
                    File.Copy(exportPath, projectPath, true);
                    
                    var metaFilePath = $"{exportPath}.meta";
                    if (File.Exists(metaFilePath)) {
                        File.Copy(metaFilePath, $"{projectPath}.meta", true);
                    }
                } catch {
                    Debug.LogError($"Failed to copy \"{asset.RelativePathToRoot}\"");
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.StopAssetEditing();
                    throw;
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            
            return UniTask.FromResult(StepResult.RestartEditor);
        }

        private static IEnumerable<AssetCatalogue.Entry> GetAllowedEntries(AssetCatalogue arAssets, AssetCatalogue projectAssets, AssetRipperSettings settings) {
            var foldersToCopy = settings.FoldersToCopy;
            var filesToExclude = settings.FilesToExcludeFromCopy;
            var filesToExcludePrefix = filesToExclude.Where(x => x.EndsWith("*")).Select(x => x[..^1]).ToArray();
            filesToExclude = filesToExclude.Except(filesToExcludePrefix).ToList();
            
            var badFiles = new List<string>();
            for (int i = 0; i < arAssets.Entries.Length; i++) {
                var asset = arAssets.Entries[i];
                
                EditorUtility.DisplayProgressBar("Getting allowed entries", $"Scrubbing {asset.RelativePathToRoot}", i / (float) arAssets.Entries.Length);
                
                if (filesToExclude.Any(x => x == asset.RelativePathToRoot)) continue;
                if (filesToExcludePrefix.Any(x => asset.RelativePathToRoot.StartsWith(x))) continue;
                
                var fileName = Path.GetFileName(asset.RelativePathToRoot);
                if (_ignoreFiles.Any(x => fileName == x)) {
                    badFiles.Add($"[badfile] {asset.RelativePathToRoot}");
                    continue;
                }
                
                if (!foldersToCopy.Any(x => asset.RelativePathToRoot.StartsWith(x))) {
                    badFiles.Add($"[notinfoldercopy] {asset.RelativePathToRoot}");
                    continue;
                }
                
                if (asset is not AssetCatalogue.ScriptEntry s) {
                    if (asset.RelativePathToRoot.EndsWith(".asmdef")) {
                        badFiles.Add($"[asmdef] {asset.RelativePathToRoot}");
                        continue;
                    }
                    
                    yield return asset;
                    continue;
                }

                // var assemblyName = s.AssemblyName;
                // if (foldersToCopy.All(x => x != assemblyName)) {
                //     var fullScriptPath = Path.Combine(arAssets.RootAssetsPath, asset.RelativePathToRoot);
                //     if (otherFoldersToCopy.All(x => !fullScriptPath.Contains(x))) {
                //         continue;   
                //     }
                // }
                
                // if (!copyFolders.Any(x => x == s.AssemblyName)) {
                //     continue;
                // } 

                // if (string.IsNullOrEmpty(x.Guid)) {
                //     // no guid, manually check for type
                //     // var fullName = s.FullTypeName;
                //     // if (allTypes.Any(x => x.FullName == fullName)) {
                //     //     continue;
                //     // }
                //     //
                //     // fullName += "`";
                //     // if (genericTypes.Any(x => x.FullName.StartsWith(fullName))) {
                //     //     continue;
                //     // }
                //     
                //     yield return x;
                //     continue;
                // }

                if (!projectAssets.ContainsFullTypeName(s)) {
                    badFiles.Add($"[type_name] {asset.RelativePathToRoot}");
                    yield return asset;
                }
            }

            foreach (var file in badFiles) {
                Debug.Log($"bad file -> {file}");
            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}