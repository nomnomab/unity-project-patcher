using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.AssetRipper;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This simply copies the AssetRipper export into the project, excluding any files that are ignored via the
    /// AssetRipperSettings asset, such as <see cref="AssetRipperSettings.FoldersToExcludeFromRead"/>.
    /// <br/><br/>
    /// If <see cref="GuidRemapperStep"/> ran, then it will re-use its asset catalogues.
    /// <br/><br/>
    /// Restarts the editor.
    /// </summary>
    public readonly struct CopyAssetRipperExportToProjectStep: IPatcherStep {
        private static readonly string[] _ignoreFiles = {
            "UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs",
            "AssemblyInfo.cs"
        };
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            var gameFolderPath = settings.ProjectGameAssetsFullPath;
            Directory.CreateDirectory(gameFolderPath);
            
            // copy the export into the proper folders
            var arAssets = GuidRemapperStep.AssetRipperCatalogue ?? AssetScrubber.ScrubDiskFolder(arSettings.OutputExportAssetsFolderPath, arSettings.FoldersToExcludeFromRead);
            var projectAssets = GuidRemapperStep.ProjectCatalogue ?? AssetScrubber.ScrubProject();

            var projectGameAssetsPath = settings.ProjectGameAssetsPath;
            
            var allowedEntries = GetAllowedEntries(arAssets, projectAssets, arSettings).ToArray();
            AssetDatabase.StartAssetEditing();
            for (var i = 0; i < allowedEntries.Length; i++) {
                var asset = allowedEntries[i];

                EditorUtility.DisplayProgressBar($"Copying assets [{i}/{allowedEntries.Length}]", $"Copying {asset.RelativePathToRoot}", i / (float)allowedEntries.Length);
                
                try {
                    var projectPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, asset, settings, arSettings, false).ToOSPath();
                    if (projectPath is null) {
                        // failed to find project path
                        // throw new System.NotImplementedException();
                        Debug.LogWarning($" - Could not find project path for \"{asset.RelativePathToRoot}\"");
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                    var exportPath = Path.Combine(arAssets.RootAssetsPath, asset.RelativePathToRoot).ToOSPath();
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

        public void OnComplete(bool failed) { }

        private static IEnumerable<AssetCatalogue.Entry> GetAllowedEntries(AssetCatalogue arAssets, AssetCatalogue projectAssets, AssetRipperSettings settings) {
            var foldersToCopy = settings.FoldersToCopy;
            var filesToExclude = settings.FilesToExcludeFromCopy;
            var filesToExcludePrefix = filesToExclude.Where(x => x.EndsWith("*")).Select(x => x.Substring(0, x.Length - 1)).ToArray();
            filesToExclude = filesToExclude.Except(filesToExcludePrefix).ToList();
            
            for (int i = 0; i < arAssets.Entries.Length; i++) {
                var asset = arAssets.Entries[i];
                
                EditorUtility.DisplayProgressBar($"Getting allowed entries [{i}/{ arAssets.Entries.Length}]", $"Scrubbing {asset.RelativePathToRoot}", i / (float) arAssets.Entries.Length);
                
                if (filesToExclude.Any(x => x == asset.RelativePathToRoot)) continue;
                if (filesToExcludePrefix.Any(x => asset.RelativePathToRoot.StartsWith(x))) continue;
                
                var fileName = Path.GetFileName(asset.RelativePathToRoot);
                if (_ignoreFiles.Any(x => fileName == x)) {
                    continue;
                }
                
                if (!foldersToCopy.Any(x => asset.RelativePathToRoot.StartsWith(x))) {
                    continue;
                }
                
                if (!(asset is AssetCatalogue.ScriptEntry)) {
                    if (asset.RelativePathToRoot.EndsWith(".asmdef")) {
                        continue;
                    }
                    
                    yield return asset;
                    continue;
                }
                
                var s = asset as AssetCatalogue.ScriptEntry;

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
                    yield return asset;
                }
            }
            
            EditorUtility.ClearProgressBar();
        }
    }
}