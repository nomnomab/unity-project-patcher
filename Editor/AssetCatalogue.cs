using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// A catalogue of assets & associated metadata information.
    /// </summary>
    [Serializable]
    public class AssetCatalogue {
        public readonly string RootAssetsPath;
        public Entry[] Entries;

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

        public IEnumerable<FoundMatch> CompareProjectToDisk(AssetCatalogue other) {
            var found = new ConcurrentBag<FoundMatch>(); 
            // var found2 = new ConcurrentBag<string>();
            var otherEntries = other.Entries;
            
            var scriptEntriesA = Entries.OfType<ScriptEntry>().ToArray();
            var scriptEntriesB = otherEntries.OfType<ScriptEntry>().ToArray();
            
            var shaderEntriesA = Entries.OfType<ShaderEntry>().ToArray();
            var shaderEntriesB = otherEntries.OfType<ShaderEntry>().ToArray();
            
            var assetEntriesA = Entries.Except(scriptEntriesA).Except(shaderEntriesA).ToArray();
            var assetEntriesB = otherEntries.Except(scriptEntriesB).Except(shaderEntriesB).ToArray();
            
            // try to match scripts
            UnityEditor.EditorUtility.DisplayProgressBar("Comparing Catalogues", "Matching scripts - This will take a while", 0);
            var each = Parallel.ForEach(scriptEntriesA, a => {
                // this becomes the path in the disk folder
                var rawPath = a.FullTypeName?.Replace('.', Path.DirectorySeparatorChar);
                rawPath = Path.Combine("Assets", "Scripts", a.AssemblyName ?? string.Empty, $"{rawPath}.cs");
                
                foreach (var b in scriptEntriesB) {
                    var bPath = Path.Combine("Assets", b.RelativePathToRoot);
                    if (rawPath != bPath) continue;
                    
                    found.Add(new FoundMatch(b, a));
                    break;
                }
            });

            while (!each.IsCompleted) { }
            
            UnityEditor.EditorUtility.ClearProgressBar();
            
            // try to match shaders
            UnityEditor.EditorUtility.DisplayProgressBar("Comparing Catalogues", "Matching shaders - This will take a while", 0);
            each = Parallel.ForEach(shaderEntriesA, a => {
                foreach (var b in shaderEntriesB) {
                    if (a.FullShaderName != b.FullShaderName) continue;
                    
                    found.Add(new FoundMatch(b, a));
                    break;
                }
            });
            
            while (!each.IsCompleted) { }
            
            UnityEditor.EditorUtility.ClearProgressBar();
            
            // try to match assets
            UnityEditor.EditorUtility.DisplayProgressBar("Comparing Catalogues", "Matching assets - This will take a while", 0);

            // figure out this step for arbitrary project files?
            var settings = PatcherUtility.GetSettings();
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            var projectGameAssetsPath = settings.ProjectGameAssetsPath;
            
            each = Parallel.ForEach(assetEntriesA, a => {
                var rawPath = Path.Combine("Assets", a.RelativePathToRoot);
                
                foreach (var b in assetEntriesB) {
                    // root folder is mapping key
                    // var bKey = b.RelativePathToRoot.Split(Path.DirectorySeparatorChar)[0];
                    // if (!arSettings.TryGetFolderMapping(bKey, out var folder)) {
                    //     continue;
                    // }
            
                    // var bPath = Path.Combine(projectGameAssetsPath, folder, Path.GetFileName(b.RelativePathToRoot));
                    if (AssetScrubber.GetProjectPathFromExportPath(projectGameAssetsPath, b, settings, arSettings, true) is not { } bPath) {
                        continue;
                    }
                    if (rawPath != bPath) continue;
                    
                    found.Add(new FoundMatch(b, a));
                    // found2.Add($"[{bKey} for {b.RelativePathToRoot}] {rawPath} <-> {bPath}");
                    break;
                }
            });
            
            while (!each.IsCompleted) { }
            
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
                    sb.AppendLine($"- <b>{(string.IsNullOrEmpty(group.Key) ? Path.DirectorySeparatorChar : group.Key)}</b>");
                } else {
                    sb.AppendLine($"- {(string.IsNullOrEmpty(group.Key) ? Path.DirectorySeparatorChar : group.Key)}");
                }

                foreach (var entry in group) {
                    sb.AppendLine($"  - {entry.ToString(withTags)}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        
        public bool TryGetEntry(string guid, out Entry entry) {
            Entry? found = Entries.FirstOrDefault(x => x.Guid == guid);
            entry = (found ?? default)!;
            return found != null;
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
                if (entry is not ScriptEntry s) continue;
                
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

            public Entry(string relativePathToRoot, string guid, long? fileId, string[]? associatedGuids) {
                if (relativePathToRoot.StartsWith("Assets")) {
                    relativePathToRoot = relativePathToRoot.Substring("Assets".Length + 1);
                }
                
                RelativePathToRoot = relativePathToRoot;
                Guid = guid;
                FileId = fileId;
                AssociatedGuids = associatedGuids ?? Array.Empty<string>();
            }

            public override string ToString() {
                return ToString(true);
            }

            public virtual string ToString(bool withTags) {
                var fileId = FileId?.ToString() ?? "n/a";
                if (withTags) {
                    return $"[{Guid}] <color={FileIdColor}>{fileId,19}</color> {RelativePathToRoot} ({AssociatedGuids.Length} associated)";
                }

                return $"[{Guid}] {fileId,19} {RelativePathToRoot} ({AssociatedGuids.Length} associated)";
            }
        }

        public class ScriptEntry : Entry {
            public string? FullTypeName;
            public string? AssemblyName;
            public ScriptEntry[] NestedTypes;
            // public bool IsGeneric;

            public ScriptEntry(string relativePathToRoot, string guid, long? fileId, string? fullTypeName, string? assemblyName, ScriptEntry[] nested, string[]? associatedGuids) : base(relativePathToRoot, guid, fileId, associatedGuids) {
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
            public string? FullShaderName;
            
            public ShaderEntry(string relativePathToRoot, string guid, long? fileId, string? fullShaderName, string[]? associatedGuids) : base(relativePathToRoot, guid, fileId, associatedGuids) {
                FullShaderName = fullShaderName;
            }
            
            public override string ToString() {
                return ToString(true);
            }
            
            public override string ToString(bool withTags) {
                return $"{base.ToString(withTags)} [{FullShaderName}]";
            }
        }
    }
}