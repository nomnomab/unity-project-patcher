using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// A catalogue of assets & associated metadata information.
    /// </summary>
    [Serializable]
    public class AssetCatalogue {
        public readonly string RootAssetsPath;
        public Entry[] Entries;
        
        public static AssetCatalogue FromDisk(string path) {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AssetCatalogue>(json);
        }
        
        [JsonConstructor]
        private AssetCatalogue() {}

        public AssetCatalogue(string rootAssetsPath, Entry[] entries) {
            RootAssetsPath = rootAssetsPath;
            Entries = entries;
        }

        public AssetCatalogue(string rootAssetsPath, IEnumerable<Entry> entries) {
            RootAssetsPath = rootAssetsPath;
            Entries = entries.ToArray();
        }

        public void InsertFrom(AssetCatalogue other) {
            Array.Resize(ref Entries, Entries.Length + other.Entries.Length);
            Array.Copy(other.Entries, 0, Entries, Entries.Length - other.Entries.Length, other.Entries.Length);
        }

        public IEnumerable<FoundMatch> CompareProjectToDisk(AssetCatalogue disk) {
            var found = new ConcurrentBag<FoundMatch>(); 
            // var found2 = new ConcurrentBag<string>();
            var otherEntries = disk.Entries;
            
            var scriptEntriesProject = Entries.OfType<ScriptEntry>().ToArray();
            var scriptEntriesDisk = otherEntries.OfType<ScriptEntry>().ToArray();
            
            var shaderEntriesProject = Entries.OfType<ShaderEntry>().ToArray();
            var shaderEntriesDisk = otherEntries.OfType<ShaderEntry>().ToArray();

            var assetEntriesProject = Entries.Except(scriptEntriesProject)
                .Except(shaderEntriesProject);
                // .GroupBy(x => x.RelativePathToRoot)
                // .Select(x => x.First());
            var assetEntriesDisk = otherEntries.Except(scriptEntriesDisk)
                .Except(shaderEntriesDisk);
                // .GroupBy(x => x.RelativePathToRoot)
                // .Select(x => x.First());
            
            // try to match scripts
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            UnityEditor.EditorUtility.DisplayProgressBar("Comparing Catalogues", "Matching scripts - This will take a while", 0);
            var each = Parallel.ForEach(scriptEntriesProject, a => {
                // this becomes the path in the disk folder
                var rawPath = a.FullTypeName?.Replace('.', Path.DirectorySeparatorChar);
                rawPath = Path.Combine("Assets", "Scripts", a.AssemblyName ?? string.Empty, $"{rawPath}.cs");
                
                foreach (var b in scriptEntriesDisk) {
                    var bPath = Path.Combine("Assets", b.RelativePathToRoot);
                    if (rawPath != bPath) continue;
                    
                    found.Add(new FoundMatch(b, a));
                    break;
                }
            });

            while (!each.IsCompleted) { }
            
            stopWatch.Stop();
            Debug.Log($"Matching scripts took {stopWatch.ElapsedMilliseconds}ms ({stopWatch.Elapsed.TotalSeconds}sec)");
            
            UnityEditor.EditorUtility.ClearProgressBar();

            // foreach (var f in found) {
            //     // Debug.Log($"Matched {f.from.Guid} <-> {f.to.Guid} for {f.from.RelativePathToRoot}");
            //     yield return f;
            // }
            //
            // yield break;
            
            // try to match shaders
            UnityEditor.EditorUtility.DisplayProgressBar("Comparing Catalogues", "Matching shaders - This will take a while", 0);
            stopWatch.Restart();
            each = Parallel.ForEach(shaderEntriesProject, a => {
                foreach (var b in shaderEntriesDisk) {
                    if (a.FullShaderName != b.FullShaderName) continue;
                    
                    found.Add(new FoundMatch(b, a));
                    // break;
                }
            });

            // var list = new List<string>();
            // for (var aa = 0; aa < shaderEntriesA.Length; aa++) {
            //     var a = shaderEntriesA[aa];
            //     EditorUtility.DisplayProgressBar("Comparing Catalogues", $"Matching shaders - {aa} / {shaderEntriesA.Length}", (float)aa / shaderEntriesA.Length);
            //     for (var bb = 0; bb < shaderEntriesB.Length; bb++) {
            //         var b = shaderEntriesB[bb];
            //         EditorUtility.DisplayProgressBar("Comparing Catalogues", $"Matching shaders - {aa} / {shaderEntriesA.Length} vs {bb} / {shaderEntriesB.Length}", (float)aa / shaderEntriesA.Length);
            //         var isMatch = a.FullShaderName == b.FullShaderName;
            //         list.Add($"[shader_test] ({isMatch}) {a} and {b}");
            //     }
            // }
            //
            // foreach (var l in list) {
            //     Debug.Log(l);
            // }
            
            // EditorUtility.ClearProgressBar();

            // throw new NotImplementedException();
            
            while (!each.IsCompleted) { }
            
            Debug.Log($"Matching shaders took {stopWatch.ElapsedMilliseconds}ms ({stopWatch.Elapsed.TotalSeconds}sec)");
            UnityEditor.EditorUtility.ClearProgressBar();
            
            // try to match assets
            UnityEditor.EditorUtility.DisplayProgressBar("Comparing Catalogues", "Matching assets - This will take a while", 0);
            
            stopWatch.Restart();

            // figure out this step for arbitrary project files?
            var settings = PatcherUtility.GetSettings();
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            var projectGameAssetsPath = settings.ProjectGameAssetsPath;

            var assetEntriesProjectGroups = assetEntriesProject.GroupBy(x => Path.GetExtension(x.RelativePathToRoot))
                .ToDictionary(x => x.Key, x => x.GroupBy(y => y.RelativePathToRoot).ToDictionary(y => Path.Combine("Assets", y.Key).ToOSPath(), y => y.First()));
            var assetEntriesDiskGroups = assetEntriesDisk.GroupBy(x => Path.GetExtension(x.RelativePathToRoot))
                .ToDictionary(x => x.Key, x => x.GroupBy(y => y.RelativePathToRoot).ToDictionary(y => y.Key.ToOSPath(), y => y.First()));

            // foreach (var a in assetEntriesProjectGroups) {
            //     Debug.Log($"[asset_test A] {a}: {string.Join(", ", a.Value.Keys)}");
            // }
            //
            // foreach (var b in assetEntriesDiskGroups) {
            //     Debug.Log($"[asset_test B] {b}: {string.Join(", ", b.Value.Keys)}");
            // }

            var index = -1;
            foreach (var extensionGroup in assetEntriesDiskGroups) {
                index++;
                var (extension, entriesA) = (extensionGroup.Key, extensionGroup.Value);
                if (EditorUtility.DisplayCancelableProgressBar("Comparing Catalogues", $"Matching assets - {extensionGroup.Key}", index / (float)assetEntriesProjectGroups.Count)) {
                    throw new OperationCanceledException();
                }

                if (!assetEntriesProjectGroups.TryGetValue(extensionGroup.Key, out var entryBs)) {
                    Debug.LogWarning($"No matching assets for extension \"{extension}\"");
                    continue;
                }

                var subIndex = -1;
                foreach (var entryGroup in entriesA) {
                    subIndex++;
                    
                    var (entryKey, entryA) = (entryGroup.Key, entryGroup.Value);
                    // var rawPath = Path.Combine("Assets", entryKey);
                    var rawPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, entryA, settings, arSettings, true).ToOSPath();
                    
                    if (EditorUtility.DisplayCancelableProgressBar("Comparing Catalogues", $"Matching assets - {entryKey}", subIndex / (float)entriesA.Count)) {
                        throw new OperationCanceledException();
                    }
                    
                    if (entryBs.TryGetValue(rawPath, out var entryB)) {
                        // var bPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, entryB, settings, arSettings, true);
                        // var bPath = Path.Combine("Assets", entryKey);
                        // if (rawPath != bPath) {
                        //     Debug.LogWarning($"Path mismatch for \"{rawPath}\" -> \"{bPath}\"");
                        //     continue;
                        // }
                        
                        found.Add(new FoundMatch(entryA, entryB));
                        // Debug.Log($"Found \"{rawPath}\" to \"{entryB.RelativePathToRoot}\"");
                    }
                    // else {
                    //     Debug.LogWarning($"No matching asset for \"{rawPath}\" -> \"{entryKey}\"");
                    // }
                    
                    // var (entryKey, entryA) = (entryGroup.Key, entryGroup.Value);
                    // var rawPath = Path.Combine("Assets", entryKey);
                    //
                    // if (EditorUtility.DisplayCancelableProgressBar("Comparing Catalogues", $"Matching assets - {entryKey}", subIndex / (float)entriesA.Count)) {
                    //     throw new OperationCanceledException();
                    // }
                    //
                    // if (entryBs.TryGetValue(entryKey, out var entryB)) {
                    //     var bPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, entryB, settings, arSettings, true);
                    //     if (rawPath != bPath) {
                    //         Debug.LogWarning($"Path mismatch for \"{rawPath}\" -> \"{bPath}\"");
                    //         continue;
                    //     }
                    //     
                    //     found.Add(new FoundMatch(entryB, entryA));
                    //     Debug.Log($"Found \"{rawPath}\" to \"{bPath}\"");
                    // } else {
                    //     Debug.LogWarning($"No matching asset for \"{rawPath}\" -> \"{entryKey}\"");
                    // }
                    
                    // subIndex++;
                    //
                    // if (EditorUtility.DisplayCancelableProgressBar("Comparing Catalogues", $"Matching assets - {entry.RelativePathToRoot}", subIndex / (float)extensionGroup.Value.Count)) {
                    //     throw new OperationCanceledException();
                    // }
                    
                    //
                    // var rawPath = Path.Combine("Assets", entry.RelativePathToRoot);
                    // if (assetEntriesBGroups.TryGetValue(a.Key, out var b)) {
                    //     foreach (var bEntry in b) {
                    //         EditorUtility.DisplayProgressBar("Comparing Catalogues", $"Matching assets - {entry.RelativePathToRoot} to {bEntry.RelativePathToRoot}", subIndex / (float)a.Value.Count);
                    //         
                    //         var bPath = AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, bEntry, settings, arSettings, true);
                    //         if (string.IsNullOrEmpty(bPath)) {
                    //             continue;
                    //         }
                    //     
                    //         if (rawPath != bPath) continue;
                    //         found.Add(new FoundMatch(bEntry, entry));
                    //     }
                    // }
                }

                index++;
            }
            
            // each = Parallel.ForEach(assetEntriesAGroups, a => {
            //     foreach (var entry in a.Value) {
            //         var rawPath = Path.Combine("Assets", entry.Key);
            //         if (assetEntriesBGroups.TryGetValue(a.Key, out var b) && b.TryGetValue(entry.Key, out var bEntry)) {
            //             if (AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, bEntry, settings, arSettings, true) is not { } bPath) {
            //                 continue;
            //             }
            //             
            //             if (rawPath != bPath) continue;
            //             found.Add(new FoundMatch(bEntry, entry.Value));
            //         }
            //     }
            //     
            //     // var rawPath = Path.Combine("Assets", a.RelativePathToRoot);
            //     // foreach (var b in assetEntriesBGroups) {
            //     //     if (AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, b, settings, arSettings, true) is not { } bPath) {
            //     //         continue;
            //     //     }
            //     //     if (rawPath != bPath) continue;
            //     //     found.Add(new FoundMatch(b, a));
            //     //     break;
            //     // }
            // });
            
            while (!each.IsCompleted) { }
            
            Debug.Log($"Matching assets took {stopWatch.ElapsedMilliseconds}ms ({stopWatch.Elapsed.TotalSeconds}sec)");
            stopWatch.Stop();
            
            UnityEditor.EditorUtility.ClearProgressBar();

            foreach (var f in found) {
                // Debug.Log($"Matched {f.from.Guid} <-> {f.to.Guid} for {f.from.RelativePathToRoot}");
                yield return f;
            }
            
            // foreach (var f in found2) {
            //     Debug.Log(f);
            // }
        }

        public struct FoundMatch {
            public Entry from;
            public Entry to;
            
            public FoundMatch(Entry from, Entry to) {
                this.from = from;
                this.to = to;
            }
            
            public override string ToString() {
                return $"[{from.GetType().Name}] Matched {from.Guid} <-> {to.Guid} for {from.RelativePathToRoot}:\n - {from}\n - {to}";
            }
        }

        public override string ToString() {
            return ToString(true);
        }

        public string ToString(bool withTags) {
            var sb = new System.Text.StringBuilder();
            if (withTags) {
                sb.AppendLine($"AssetCatalogue of {Entries.Length} entries <i>(click for details)</i>");
                sb.AppendLine($"<b>\"Assets\" root</b>: {RootAssetsPath}");
            } else {
                sb.AppendLine($"AssetCatalogue of {Entries.Length} entries (click for details)");
                sb.AppendLine($"\"Assets\" root");
            }
            sb.AppendLine();

            // group by folder
            var groups = Entries.GroupBy(x => Path.GetDirectoryName(x.RelativePathToRoot));

            // build tree
            foreach (var group in groups) {
                if (withTags) {
                    sb.AppendLine($"- <b>{(string.IsNullOrEmpty(group.Key) ? Path.DirectorySeparatorChar.ToString() : group.Key)}</b>");
                } else {
                    sb.AppendLine($"- {(string.IsNullOrEmpty(group.Key) ? Path.DirectorySeparatorChar.ToString() : group.Key)}");
                }

                foreach (var entry in group) {
                    sb.AppendLine($"  - {entry.ToString(withTags)}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        
        public bool TryGetEntry(string guid, out Entry entry) {
#if UNITY_2020_3_OR_NEWER
            Entry? found = Entries.FirstOrDefault(x => x.Guid == guid);
            entry = (found ?? default)!;
#else
            var found = Entries.FirstOrDefault(x => x.Guid == guid);
            entry = found ?? default;
#endif
            return found != null;
        }
        
        public Dictionary<string, Entry> ToLookupByFileName() {
            var lookup = new Dictionary<string, Entry>();
            foreach (var entry in Entries) {
                lookup[Path.GetFileName(entry.RelativePathToRoot)] = entry;
            }
            return lookup;
        }

        public IEnumerable<Entry> GetAssociatedEntries(Entry entry) {
            foreach (var guid in entry.AssociatedGuids) {
                if (TryGetEntry(guid, out var e)) {
                    yield return e;
                }
            }
        }
        
        public bool ContainsFullTypeName(ScriptEntry otherEntry) {
            foreach (var entry in Entries) {
                if (!(entry is ScriptEntry)) continue;
                
                var s = (ScriptEntry)entry;
                
                // check direct type name
                if (s.FullTypeName == otherEntry.FullTypeName) {
                    return true;
                }
                
                // check nested types
                if (s.NestedTypes.Any(x => x.FullTypeName == otherEntry.FullTypeName)) {
                    return true;
                }
            }
            
            return false;
        }

        public class Entry {
            private const string FileIdColor = "#c0c0c07f";

            public string RelativePathToRoot;
            public string Guid;
            public long? FileId;
            public string[] AssociatedGuids;
            public string[] FileIds;

#if UNITY_2020_3_OR_NEWER
            public Entry(string relativePathToRoot, string guid, long? fileId, string[]? associatedGuids, string[]? fileIds) {
#else
            public Entry(string relativePathToRoot, string guid, long? fileId, string[] associatedGuids, string[] fileIds) {
#endif
                if (relativePathToRoot.StartsWith("Assets")) {
                    relativePathToRoot = relativePathToRoot.Substring("Assets".Length + 1);
                }
                
                RelativePathToRoot = relativePathToRoot;
                Guid = guid;
                FileId = fileId;
                AssociatedGuids = (associatedGuids ?? Array.Empty<string>()).Distinct().ToArray();
                FileIds = fileIds;
            }

            public override string ToString() {
                return ToString(true);
            }

            public virtual string ToString(bool withTags) {
                var fileId = FileId?.ToString() ?? "n/a";
                var fileIdString = FileIds == null || FileIds.Length == 0 ? string.Empty : $"\n{string.Join("\n", FileIds.Select(x => $" \t> fileID: {x}"))}";
                
                if (withTags) {
                    return $"[{Guid}] <color={FileIdColor}>{fileId,19}</color> {RelativePathToRoot} ({AssociatedGuids.Length} associated){fileIdString}";
                }

                return $"[{Guid}] {fileId,19} {RelativePathToRoot} ({AssociatedGuids.Length} associated){fileIdString}";
            }
        }

        public class ScriptEntry : Entry {
#if UNITY_2020_3_OR_NEWER
            public string? FullTypeName;
            public string? AssemblyName;
#else
            public string FullTypeName;
            public string AssemblyName;
#endif
            public ScriptEntry[] NestedTypes;
            // public bool IsGeneric;

#if UNITY_2020_3_OR_NEWER
            public ScriptEntry(string relativePathToRoot, string guid, long? fileId, string? fullTypeName, string? assemblyName, ScriptEntry[] nested, string[]? associatedGuids, string[]? fileIds) : base(relativePathToRoot, guid, fileId, associatedGuids, fileIds) {
#else
            public ScriptEntry(string relativePathToRoot, string guid, long? fileId, string fullTypeName, string assemblyName, ScriptEntry[] nested, string[] associatedGuids, string[] fileIds) : base(relativePathToRoot, guid, fileId, associatedGuids, fileIds) {
#endif
                FullTypeName = fullTypeName;
                AssemblyName = assemblyName;
                // IsGeneric = isGeneric;
                NestedTypes = nested;
            }

            public override string ToString() {
                return ToString(true);
            }

            public override string ToString(bool withTags) {
                return $"{base.ToString(withTags)} [{AssemblyName}] [{FullTypeName}]\n\t* {string.Join("\n\t* ", NestedTypes.Select(x => x.ToString(withTags)))}";
            }
        }

        public class ShaderEntry : Entry {
#if UNITY_2020_3_OR_NEWER
            public string? FullShaderName;
#else
            public string FullShaderName;
#endif
            
#if UNITY_2020_3_OR_NEWER
            public ShaderEntry(string relativePathToRoot, string guid, long? fileId, string? fullShaderName, string[]? associatedGuids, string[]? fileIds) : base(relativePathToRoot, guid, fileId, associatedGuids, fileIds) {
#else
            public ShaderEntry(string relativePathToRoot, string guid, long? fileId, string fullShaderName, string[] associatedGuids, string[] fileIds) : base(relativePathToRoot, guid, fileId, associatedGuids, fileIds) {
#endif
                FullShaderName = fullShaderName;
            }
            
            public override string ToString() {
                return ToString(true);
            }
            
            public override string ToString(bool withTags) {
                return $"{base.ToString(withTags)} [{FullShaderName}]";
            }
        }

        public string ToJson() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}