using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Gathers up all asset guids in the asset ripper export, and attempts to remap them to the corresponding
    /// assets in the project. This can take a while for large projects.
    /// <br/><br/>
    /// This step also attempts to remap all bundle names that are guids into their non-guid names.
    /// <br/><br/>
    /// This step <b>requires</b>:
    /// <br/> - <see cref="AssetRipperStep"/>
    /// <br/> - <see cref="PromptUserWithAddressablePluginStep"/>
    /// </summary>
    public readonly struct AddressablesGuidRemapperStep: IPatcherStep {
        private readonly static Regex AddressableGuidPattern = new Regex(@"m_AssetGUID:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled);
        private readonly static Regex AssetBundleGuidPattern = new Regex(@"assetBundleName:\s(?<name>[0-9A-Za-z]+)", RegexOptions.Compiled);
        private readonly static string[] IgnoreFileExtensionsForAddressableAssetScrubbing = {
            ".meta",
            ".ogg",
            // ".controller",
            // ".anim",
            ".mat",
            ".overrideController",
            // ".asset",
            // ".unity",
            ".webm",
            ".bundle",
            ".shader",
            ".cs",
            ".asmdef",
            ".png",
            // ".prefab",
            ".ttf",
            ".otf",
            ".h",
            ".cpp",
            ".mask",
            ".svg",
            ".Z",
            ".json",
            ".dat",
            ".so",
            ".xml",
            ".pbs",
            ".txt",
            ".renderTexture",
            // ".mixer",
            ".dll",
            ".exe",
            ".md",
            ".cff",
            ".physicMaterial",
        };

        public UniTask<StepResult> Run() {
            var arSettings = this.GetAssetRipperSettings();
            var assetsPath = arSettings.OutputExportAssetsFolderPath;
            
            EditorUtility.DisplayProgressBar("Scrubbing Addressables", "Gathering files...", 0);
            
            var files = Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
                .Where(x => !IgnoreFileExtensionsForAddressableAssetScrubbing.Any(y => y == Path.GetExtension(x)))
                .ToArray();
            
            EditorUtility.DisplayProgressBar("Scrubbing Addressables", "Finding \"m_AssetGUID\" in files...", 0);
            
            var lookup = new ConcurrentBag<(string file, string fileGuid, List<(string from, string to)>)>();
            var each = Parallel.ForEach(files, file => {
                var contents = File.ReadAllText(file.ToValidPath());
                var matches = AddressableGuidPattern.Matches(contents);
                // if (matches.Count == 0) return;
            
                var metaGuid = AssetScrubber.GetGuidFromDisk(file);
                var list = new List<(string, string)>();
                foreach (var match in matches.Cast<Match>()) {
                    list.Add((match.Groups["guid"].Value, null));
                }
                lookup.Add((file, metaGuid, list));
            });
            
            // foreach (var (file, fileGuid, list) in lookup) {
            //     Debug.Log($"[lookup] \"{file}\" of {fileGuid} -> {list.Count}");
            // }
            
            var settings = PatcherUtility.GetSettings();
            var keysPath = Path.Combine(settings.GameDataPath, "Addressables_Rip", "0_ResourceLocationMap_AddressablesMainContentCatalog_keys.txt");
            var addressableIdLookup = new Dictionary<string, string>(); // <guid, addressable_path>
            using (var reader = new StreamReader(keysPath)) {
                string lastLine = string.Empty;
                string line;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
            
                    if (EditorUtility.DisplayCancelableProgressBar("Scrubbing Addressables", line, 0)) {
                        throw new OperationCanceledException();
                    }
            
                    if (line.Length == 32) {
                        addressableIdLookup[line] = lastLine;
                    }
                    
                    lastLine = line;
                }
            }
            
            // foreach (var pair in addressableIdLookup) {
            //     Debug.Log($"\"{pair.Key}\": \"{pair.Value}\"");
            // }
            
            var nameLookup = new Dictionary<string, string>(); // <addressable_path, guid>
            foreach (var (file, fileGuid, list) in lookup) {
                nameLookup[Path.GetFileName(file)] = fileGuid;
            }
            
            // foreach (var pair in nameLookup) {
            //     Debug.Log($"[nameLookup] \"{pair.Key}\": \"{pair.Value}\"");
            // }
            
            var index = -1;
            // var debugs = new ConcurrentBag<string>();
            foreach (var (file, fileGuid, allGuids) in lookup) {
                var name = Path.GetFileName(file);
                
                index++;
                if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing Addressables [{index}/{lookup.Count}]", $"Checking {name}", index / (float)lookup.Count)) {
                    throw new OperationCanceledException();
                }
                if (allGuids.Count == 0) continue;
                
                if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing Addressables [{index}/{lookup.Count}]", $"Fixing {name}", index / (float)lookup.Count)) {
                    throw new OperationCanceledException();
                }
            
                var fileLines = File.ReadAllLines(file);
            
                EditorUtility.DisplayProgressBar($"Scrubbing Addressables", $"Scanning {fileLines.Length} lines...", index / (float)lookup.Count);
                each = Parallel.For(0, fileLines.Length, (int i) => {
                    var line = fileLines[i];
                    if (!line.Contains("m_AssetGUID:")) return;
            
                    var matches = AddressableGuidPattern.Matches(line);
                    if (matches.Count == 0) return;
                    
                    var matchIndex = -1;
                    foreach (var match in matches.Cast<Match>()) {
                        matchIndex++;
                        var guid = match.Groups["guid"].Value;
                        if (addressableIdLookup.TryGetValue(guid, out var addressablePath)) {
                            var fileName = Path.GetFileName(addressablePath);
                            if (!nameLookup.TryGetValue(fileName, out var newGuid)) {
                                // debugs.Add($"[none]  - \"{guid}\"");
                                continue;
                            }
                            line = line.Replace(guid, newGuid);
                            fileLines[i] = line;
                            // debugs.Add($"  - \"{guid}\" -> \"{newGuid}\"");
                        }
                    }
                });
            
                File.WriteAllLines(file, fileLines);
            }
            
            // foreach (var debug in debugs) {
            //     Debug.Log(debug);
            // }
            
            // EditorUtility.ClearProgressBar();
            // return UniTask.FromResult(StepResult.Success);
            
            EditorUtility.DisplayProgressBar("Scrubbing Addressables", "Gathering files...", 0);
            
            files = Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == ".meta" && !Path.GetFileName(x).EndsWith(".cs.meta"))
                .Where(x => !Path.GetFileName(x).StartsWith("pb_Mesh-"))
                .ToArray();
            
            var locationsPath = Path.Combine(settings.GameDataPath, "Addressables_Rip", "0_ResourceLocationMap_AddressablesMainContentCatalog_locations.txt");
            var locationsLookup = new ConcurrentDictionary<string, string>(); // <guid, addressable_path>
            var lines = File.ReadAllLines(locationsPath);

            EditorUtility.DisplayProgressBar("Scrubbing Addressables", "Scanning list for good bundle names...", 0);
            
            // 0: path
            // 1: internal_id
            // 2: guid
            for (int i = 0; i < lines.Length; i += 3) {
                if (i + 2 >= lines.Length) break;
                
                var path = lines[i];
                var line = lines[i + 2].Trim();
                var pathName = Path.GetFileNameWithoutExtension(path);
                locationsLookup[line] = pathName;
            }
            
            // foreach (var pair in locationsLookup) {
            //     Debug.Log($"\"{pair.Key}\": \"{pair.Value}\"");
            // }
            
            EditorUtility.DisplayProgressBar("Scrubbing Addressables", "Scanning files for bad bundle names...", 0);
            
            for (var i = 0; i < files.Length; i++) {
                var file = files[i];
                if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing Addressables [{i}/{files.Length}]", $"Scanning {file} for bad bundle names...", i / (float)files.Length)) {
                    throw new OperationCanceledException();
                }
            
                lines = File.ReadAllLines(file);
                each = Parallel.For(0, lines.Length, (int f) => {
                    var line = lines[f];
                    if (!line.Contains("assetBundleName:")) return;
            
                    var matches = AssetBundleGuidPattern.Matches(line);
                    if (matches.Count == 0) return;
            
                    var matchIndex = -1;
                    foreach (var match in matches.Cast<Match>()) {
                        matchIndex++;
                        var idName = match.Groups["name"].Value;
                        if (locationsLookup.TryGetValue(idName, out var newName)) {
                            line = line.Replace(idName, newName);
                            lines[f] = line;
                        }
                    }
                });
                
                File.WriteAllLines(file, lines);
            }

            // var arCatalogue = GuidRemapperStep.AssetRipperCatalogue ?? AssetScrubber.ScrubDiskFolder(arSettings.OutputExportAssetsFolderPath, arSettings.FoldersToExcludeFromRead);
            // var arLookup = arCatalogue.ToLookupByFileName();
            // var index = -1;
            // foreach (var foundFiles in lookup) {
            //     index++;
            //     
            //     EditorUtility.DisplayProgressBar("Scrubbing Addressables", $"Checking {foundFiles.file}", index / (float) lookup.Count);
            //     
            //     var (file, fileGuid, list) = foundFiles;
            //     var changedFile = false;
            //     for (var i = 0; i < list.Count; i++) {
            //         var guid = list[i];
            //         
            //         EditorUtility.DisplayProgressBar("Scrubbing Addressables", $"Checking {foundFiles.file} with {guid}", 0);
            //         
            //         if (!addressableIdLookup.TryGetValue(guid.from, out var path)) {
            //             continue;
            //         }
            //         
            //         var fileName = Path.GetFileName(path);
            //         if (arLookup.TryGetValue(fileName, out var entry)) {
            //             var pair = list[i];
            //             pair.to = entry.Guid;
            //             list[i] = pair;
            //             changedFile = true;
            //         }
            //     }
            //     
            //     if (changedFile) {
            //         var contents = File.ReadAllText(file);
            //         // faster than raw string replacement?
            //         var sb = new StringBuilder(contents);
            //         foreach (var guid in list) {
            //             if (string.IsNullOrEmpty(guid.to)) continue;
            //             sb = sb.Replace($"m_AssetGUID: {guid.from}", $"m_AssetGUID: {guid.to}");
            //         }
            //         File.WriteAllText(file, sb.ToString());
            //     }
            // }
            
            EditorUtility.ClearProgressBar();
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}