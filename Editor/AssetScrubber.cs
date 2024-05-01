using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nomnom.UnityProjectPatcher.AssetRipper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Nomnom.UnityProjectPatcher.Editor {
    public static class AssetScrubber {
        private readonly static string[] _emptySearchFolder = Array.Empty<string>();
        private readonly static string[] _assetsRootSearchFolder = new[] {
            "Assets"
        };
        
        // private readonly static string[] IgnoreScriptFolders = {
        //     "Unity.Services",
        //     "Unity.Timeline",
        //     "Unity.Multiplayer",
        //     // "Unity.InputSystem",
        //     "Unity.Burst",
        //     // "DissonanceVoip",
        //     // "Facepunch",
        //     // "Unity.Collections",
        //     // "Unity.Jobs",
        //     // "Unity.Networking",
        //     // "Unity.ProBuilder"
        // };

        private readonly static string[] IgnoreContains = {
            "Editor",
            "Test",
        };
        
        private readonly static string[] IgnoreDiskContains = {
            "Editor",
            // "Test",
            "Profile-",
            "Profiling.",
            "Unity.Services."
            // "Unity."
        };
        
        private readonly static string[] IgnoreEndsWith = {
            // "Attribute.cs",
        };

        private readonly static string[] IgnoreFileExtensionsForAssetAssociations = {
            ".png",
            ".jpg",
            ".jpeg",
            ".tga",
            ".tif",
            ".tiff",
            ".dds",
            ".hdr",
            ".exr",
            ".psd",
            ".mp4",
            ".cs",
        };
        
        private readonly static Regex GuidPattern = new(@"guid:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled);

        [MenuItem("Tools/Scrub/Assets")]
        public static void TestScrubProjectAssets() {
            var catalogue = ScrubProjectAssets();
            Debug.Log(catalogue);

            var outputPath = Path.Combine(Application.dataPath, "scrub.assets.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Scrub/Project")]
        public static void TestScrubProject() {
            var catalogue = ScrubProject();
            Debug.Log(catalogue);
            
            var outputPath = Path.Combine(Application.dataPath, "scrub.project.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Scrub/Disk")]
        public static void TestScrubDiskFolder() {
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            var catalogue = ScrubDiskFolder(Application.dataPath, arSettings.FoldersToExcludeFromRead);
            Debug.Log(catalogue);
            
            var outputPath = Path.Combine(Application.dataPath, "scrub.disk.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Scrub/Disk - Custom")]
        public static void TestScrubDiskFolderCustom() {
            var disk = EditorUtility.OpenFolderPanel("Scrub Folder", "Assets", "");
            if (string.IsNullOrEmpty(disk)) return;
            
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            var catalogue = ScrubDiskFolder(disk, arSettings.FoldersToExcludeFromRead);
            Debug.Log(catalogue);
            
            var outputPath = Path.Combine(Application.dataPath, "scrub.disk_custom.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Scrub/Compare")]
        public static void TestScrubCompareTwoProjects() {
            var disk = EditorUtility.OpenFolderPanel("Scrub Folder", "Assets", "");
            if (string.IsNullOrEmpty(disk)) return;
            
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            var diskCatalogue = ScrubDiskFolder(disk, arSettings.FoldersToExcludeFromRead);
            var projectCatalogue = ScrubProject();
            
            Debug.Log(diskCatalogue.ToString(false));
            Debug.Log(projectCatalogue.ToString(false));
            foreach (var match in projectCatalogue.CompareProjectToDisk(diskCatalogue)) {
                Debug.Log(match);
            }
        }
        
        public static AssetCatalogue ScrubProjectAssets() {
            return ScrubProject(string.Empty, _assetsRootSearchFolder);
        }

        public static AssetCatalogue ScrubProject() {
            return ScrubProject(string.Empty, _emptySearchFolder);
        }

        public static AssetCatalogue ScrubProject(string searchQuery, string[] searchInFolders) {
            for (var i = 0; i < searchInFolders.Length; i++) {
                searchInFolders[i] = searchInFolders[i].ToAssetDatabaseSafePath();
            }
            
            using var _ = ListPool<AssetCatalogue.Entry>.Get(out var entries);
            
            EditorUtility.DisplayProgressBar("Scrubbing Project", "Scrubbing Assets", 0);

            var assetGuids = AssetDatabase.FindAssets(searchQuery, searchInFolders);
            using var __ = ListPool<(MonoScript, string assetPath, long fileId)>.Get(out var nonMonos);
            using var ___ = HashSetPool<string>.Get(out var usedTypes);
            
            for (var i = 0; i < assetGuids.Length; i++) {
                var assetGuid = assetGuids[i];
                if (EditorUtility.DisplayCancelableProgressBar("Scrubbing Project", $"Scrubbing {assetGuid}", i / (float)assetGuids.Length)) {
                    throw new OperationCanceledException();
                }
                
                // load type information
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (!Path.HasExtension(assetPath)) continue;
                
                // exclude
                if (IgnoreContains.Any(x => assetPath.Contains(x))) continue;
                if (IgnoreEndsWith.Any(x => assetPath.EndsWith(x))) continue;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType is null) continue;

                UnityEngine.Object? obj = null;
                try {
                    // materials are dumb and tend to crash when imported???
                    if (assetPath.EndsWith(".mat")) {
                        var materialGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        entries.Add(new AssetCatalogue.Entry(assetPath.Replace('/', Path.DirectorySeparatorChar), materialGuid, 4800000, GetAssociatedGuids(Path.GetFullPath(assetPath)).ToArray()));
                        continue;
                    }

                    // shaders are dumb too???
                    if (assetPath.EndsWith(".shader")) {
                        var shaderType = GetShaderName(Path.GetFullPath(assetPath));
                        var shaderGuid = AssetDatabase.AssetPathToGUID(assetPath);
                        entries.Add(new AssetCatalogue.ShaderEntry(assetPath.Replace('/', Path.DirectorySeparatorChar), shaderGuid, 4800000, shaderType, null));
                        continue;
                    }
                    
                    if (!AssetDatabase.IsMainAssetAtPathLoaded(assetPath)) {
                        obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    } else {
                        obj = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
                    }
                    
                    var found = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long fileId);
                    if (!found) continue;
                    
                    var associatedGuids = GetAssociatedGuids(Path.GetFullPath(assetPath)).ToArray();

                    if (assetType != typeof(MonoScript)) {
                        if (assetType == typeof(Shader)) {
                            var shaderType = GetShaderName(Path.GetFullPath(assetPath));
                            entries.Add(new AssetCatalogue.ShaderEntry(assetPath.Replace('/', Path.DirectorySeparatorChar), guid, fileId, shaderType, associatedGuids));
                        } else {
                            entries.Add(new AssetCatalogue.Entry(assetPath.Replace('/', Path.DirectorySeparatorChar), guid, fileId, associatedGuids));
                        }
                    } else {
                        var monoScript = obj as MonoScript;
                        if (monoScript is null) continue;
                        
                        var foundClass = monoScript.GetClass();
                        nonMonos.Add((monoScript, assetPath.Replace('/', Path.DirectorySeparatorChar), fileId));
                        // if (assetType == typeof(MonoScript)) {
                        //     // Debug.Log($"\"{assetPath}\" -> \"{foundClass}\" vs \"{assetType}\"");
                        //     nonMonos.Add((monoScript, assetPath.Replace('/', Path.DirectorySeparatorChar), fileId));
                        // }
                        
                        var assemblyName = foundClass?.Assembly.GetName().Name;
                        // var isGeneric = foundClass?.IsGenericType ?? false;

                        var nestedTypes = foundClass?.GetNestedTypes()
                            .Select(x => new AssetCatalogue.ScriptEntry(x.FullName ?? "n/a", "n/a", null, x.FullName ?? "n/a", assemblyName ?? "Assembly-CSharp", Array.Empty<AssetCatalogue.ScriptEntry>(), null))
                            .ToArray() ?? Array.Empty<AssetCatalogue.ScriptEntry>();

                        var typeName = foundClass?.FullName ?? assetType.FullName ?? "n/a";
                        entries.Add(new AssetCatalogue.ScriptEntry(assetPath.Replace('/', Path.DirectorySeparatorChar), guid, fileId, foundClass?.FullName ?? assetType.FullName ?? "n/a", assemblyName ?? "Assembly-CSharp", nestedTypes, associatedGuids));
                        
                        usedTypes.Add(typeName);
                        
                        foreach (var nestedType in nestedTypes) {
                            if (nestedType is null || nestedType.FullTypeName is null) {
                                continue;
                            }
                            
                            usedTypes.Add(nestedType.FullTypeName);
                        }
                    }
                } catch (Exception e) {
                    Debug.LogError($"Failed to load {assetPath}.\n{e}");
                }
                finally {
                    if (obj && obj is not GameObject or Component or AssetBundle) {
                        Resources.UnloadAsset(obj);
                    }
                }
            }
            
            var allTypes = GetAllTypes();
            
            // things that aren't MonoBehaviour but still are found
            for (var i = 0; i < nonMonos.Count; i++) {
                var (nonMono, assetPath, fileId) = nonMonos[i];
                if (!nonMono) continue;

                if (EditorUtility.DisplayCancelableProgressBar("Scrubbing Project", $"Scrubbing nonMono {nonMono.name}", i / (float)nonMonos.Count)) {
                    throw new OperationCanceledException();
                }

                foreach (var entry in ScrubNonMonoData(nonMono, assetPath, fileId, allTypes)) {
                    // if (entries.Any(x => x is AssetCatalogue.ScriptEntry s && entry is AssetCatalogue.ScriptEntry s2 && s.FullTypeName == s2.FullTypeName)) {
                    //     continue;
                    // }
                    if (usedTypes.Contains(entry.FullTypeName ?? string.Empty)) {
                        continue;
                    }
                    
                    entries.Add(entry);
                }

                if (nonMono) {
                    Resources.UnloadAsset(nonMono);
                }
            }

            GC.Collect();
            EditorUtility.ClearProgressBar();

            return new AssetCatalogue(Application.dataPath, entries);
        }

        private static Type[] GetAllTypes() {
            var scriptAssembliesPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies");
            var dlls = Directory.GetFiles(scriptAssembliesPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => !IgnoreDiskContains.Any(x.Contains));
                // .ToArray();
            
            // foreach (var dll in dlls) {
            //     Debug.Log($"Scrubbing {dll}");
            // }
            
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && dlls.Contains(x.GetName().Name))
                .SelectMany(x => x.GetTypes())
                .ToArray();

            return allTypes;
        }
        
        public static string? GetShaderName(string assetPath) {
            if (!File.Exists(assetPath)) {
                return null;
            }
            
            var assetContents = File.ReadAllText(assetPath);
            var regex = new Regex(@"Shader\s+.(?<TypeName>.*\w)");
            var match = regex.Match(assetContents);
            if (!match.Success) {
                return null;
            }
            
            return match.Groups["TypeName"].Value.Trim();
        }
        
        public static IEnumerable<AssetCatalogue.ScriptEntry> ScrubNonMonoData(MonoScript nonMono, string assetPath, long fileId) {
            return ScrubNonMonoData(nonMono, assetPath, fileId, GetAllTypes());
        }

        public static IEnumerable<AssetCatalogue.ScriptEntry> ScrubNonMonoData(MonoScript nonMono, string assetPath, long fileId, Type[] allTypes) {
            // Debug.Log($"dlls: {string.Join(", ", dlls)}");
            // Debug.Log($"all dlls: {string.Join(", ", AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name).OrderBy(x => x))}");
            // Debug.Log($"allTypes: {allTypes.Length}");
            
            var text = nonMono.text;
            //! how the fuck do I optimize this...?
            var foundNamespace = GetNamespace(text);
            var foundClasses = GetDefinitions(text, "class");
            var foundStructs = GetDefinitions(text, "struct");
            var foundInterfaces = GetDefinitions(text, "interface");
            var foundEnums = GetDefinitions(text, "enum");
            var foundDelegates = GetDelegateDefinitions(text);

            var combinedTypes = foundClasses.Select(x => $"{foundNamespace}.{x}")
                .Concat(foundStructs.Select(x => $"{foundNamespace}.{x}"))
                .Concat(foundInterfaces.Select(x => $"{foundNamespace}.{x}"))
                .Concat(foundEnums.Select(x => $"{foundNamespace}.{x}"))
                .Concat(foundDelegates.Select(x => $"{foundNamespace}.{x}"))
                .ToArray();

            foreach (var type in combinedTypes) {
                var existingType = allTypes.FirstOrDefault(x => x.FullName == type || (x.IsGenericType && x.GetGenericTypeDefinition() is { } generic && (generic.FullName ?? String.Empty).Equals(type, StringComparison.Ordinal)));
                if (existingType is null) {
                    Debug.LogWarning($"Could not find type \"{type}\"!");
                    continue;
                }

                yield return new AssetCatalogue.ScriptEntry(assetPath, string.Empty, fileId, existingType.FullName, existingType.Assembly.GetName().Name, Array.Empty<AssetCatalogue.ScriptEntry>(), null);
            }
        }

        private static string? GetNamespace(string text) {
            var namespaceRegex = new Regex(@"namespace\s+(\w.*)");
            var match = namespaceRegex.Match(text);
            
            if (!match.Success) return null;
            return match.Groups[1].Value.Trim();
        }

        private static IEnumerable<string> GetDelegateDefinitions(string text) {
            var regex = new Regex(@"delegate\s+\w+\s+(?<TypeName>\w+)");
                
            // no generics
            foreach (var match in regex.Matches(text).Cast<Match>()) {
                if (!match.Success) continue;
                yield return match.Groups["TypeName"].Value.Trim();
            }
        }
        
        private static IEnumerable<string> GetDefinitions(string text, string name) {
            var regexGeneric = new Regex(@$"{name}\s+(?<TypeName>\w+)(?:\s*<(?<GenericParameters>\w+(?:,\s*\w+)*)?>)");
            
            // generics
            foreach (var match in regexGeneric.Matches(text).Cast<Match>()) {
                if (!match.Success) continue;

                var typeName = match.Groups["TypeName"];
                var genericParameters = match.Groups["GenericParameters"];

                if (!genericParameters.Success) {
                    yield return typeName.Value.Trim();
                    continue;
                }

                var genericCount = genericParameters.Value.Count(x => x == ',') + 1;
                yield return $"{typeName.Value.Trim()}`{genericCount}";
            }
            
            var regex = new Regex(@$"{name}\s+(?<TypeName>\w+)");

            // no generics
            foreach (var match in regex.Matches(text).Cast<Match>()) {
                if (!match.Success) continue;
                yield return match.Groups["TypeName"].Value.Trim();
            }
        }

        public static AssetCatalogue ScrubDiskFolder(string folderPath, IEnumerable<string> foldersToExclude) {
            using var _ = ListPool<AssetCatalogue.Entry>.Get(out var entries);
            
            EditorUtility.DisplayProgressBar("Scrubbing Folder", $"Scrubbing {folderPath}", 0);
            
            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++) {
                var file = files[i];
                if (!File.Exists(file)) continue;
                if (Path.GetExtension(file) == ".meta") continue;
                if (IgnoreEndsWith.Any(x => file.EndsWith(x))) continue;

                if (EditorUtility.DisplayCancelableProgressBar("Scrubbing Folder", $"Scrubbing {file}", i / (float)files.Length)) {
                    throw new OperationCanceledException();
                }

                var extension = Path.GetExtension(file).ToLowerInvariant();
                var isScript = extension == ".cs";
                var isShader = extension == ".shader";
                var guid = GetGuidFromDisk(file) ?? string.Empty;
                // no meta found
                // if (guid is null) continue;

                var finalFile = file[(folderPath.Length + 1)..];
                if (foldersToExclude.Any(x => finalFile.StartsWith(x))) continue;
                
                // exclude
                if (IgnoreDiskContains.Any(x => finalFile.Contains(x))) continue;
                
                var associatedGuids = GetAssociatedGuids(file).ToArray();
                if (isScript) {
                    //! I hate this :)
                    // trim root folder
                    var rootFolder = finalFile[..(finalFile.IndexOf('\\'))];
                    // if (rootFolder != "Scripts") continue;
                    var typeName = finalFile[(rootFolder.Length + 1)..];
                    
                    // take first folder
                    var assemblyName = typeName[..(typeName.IndexOf('\\'))];
                    // if (foldersToExclude.Contains(assemblyName)) continue;
                    
                    // trim first folder
                    var fullTypeName = typeName[(assemblyName.Length + 1)..];
                    // trim extension
                    fullTypeName = fullTypeName[..(fullTypeName.LastIndexOf('.'))];
                    // make a type path
                    fullTypeName = fullTypeName.Replace('\\', '.');

                    var contents = File.ReadAllText(file);
                    var foundNamespace = GetNamespace(contents);
                    foreach (var e in GetDefinitions(contents, "class").Concat(GetDefinitions(contents, "struct")).Take(1)) {
                        fullTypeName = $"{foundNamespace}.{e}";
                    }

                    // var isGeneric = IsGenericScript(file, Path.GetFileNameWithoutExtension(finalFile));
                    entries.Add(new AssetCatalogue.ScriptEntry(finalFile, guid, null, fullTypeName, assemblyName, Array.Empty<AssetCatalogue.ScriptEntry>(), associatedGuids));
                    continue;
                }

                if (isShader) {
                    var shaderType = GetShaderName(file);
                    entries.Add(new AssetCatalogue.ShaderEntry(finalFile, guid, null, shaderType, associatedGuids));
                    // Debug.Log($" - \"{finalFile}\" -> \"{shaderType}\"");
                    continue;
                }

                entries.Add(new AssetCatalogue.Entry(finalFile, guid, null, associatedGuids));
            }
            
            EditorUtility.ClearProgressBar();

            return new AssetCatalogue(folderPath, entries);
        }
        
        private static bool IsGenericScript(string assetPath, string typeName) {
            if (!File.Exists(assetPath)) return false;
            var assetContents = File.ReadAllText(assetPath);
            return assetContents.Contains($" {typeName}<");
        }

        private static string? GetGuidFromDisk(string assetPath) {
            // scrub meta file directly (?)
            // var fullAssetPath = Path.GetFullPath(assetPath);
            var fullMetaPath = $"{assetPath}.meta";
            if (!File.Exists(fullMetaPath)) return null;
                
            var metaContents = File.ReadAllText(fullMetaPath);
            var match = GuidPattern.Match(metaContents);
            if (!match.Success) return null;
                
            // grab guid from disk
            var actualGuid = match.Groups["guid"].Value;
            if (string.IsNullOrEmpty(actualGuid)) return null;

            return actualGuid;
        }
        
        private static IEnumerable<string> GetAssociatedGuids(string assetPath) {
            // var fullMetaPath = $"{fullAssetPath}.meta";
            if (!File.Exists(assetPath)) {
                yield break;
            }
                
            var assetContents = File.ReadAllText(assetPath);
            var matches = GuidPattern.Matches(assetContents);
            
            foreach (var match in matches.Cast<Match>()) {
                if (!match.Success) continue;
                
                var guid = match.Groups["guid"].Value;
                if (!string.IsNullOrEmpty(guid)) {
                    yield return guid;
                }
            }
        }

        public static void ReplaceMetaGuid(string rootPath, AssetCatalogue.Entry entry, string guid) {
            var fullAssetPath = Path.Combine(rootPath, entry.RelativePathToRoot);
            var fullMetaPath = $"{fullAssetPath}.meta";
            if (!File.Exists(fullMetaPath)) {
                Debug.LogWarning($"Could not find \"{fullMetaPath}\"");
                return;
            }

            var contents = File.ReadAllText(fullMetaPath);
            var match = GuidPattern.Match(contents);
            if (!match.Success) return;
            
            var newContents = contents.Replace(match.Groups["guid"].Value, guid);
            File.WriteAllText(fullMetaPath, newContents);
            
            Debug.Log($" - wrote to {fullMetaPath}");
        }

        public static void ReplaceAssetGuids(string rootPath, AssetCatalogue.Entry entry, Dictionary<string, AssetCatalogue.FoundMatch> guids) {
            var fileExtension = Path.GetExtension(entry.RelativePathToRoot);
            if (IgnoreFileExtensionsForAssetAssociations.Contains(fileExtension)) {
                return;
            }
            
            var fullAssetPath = Path.Combine(rootPath, entry.RelativePathToRoot);
            if (!File.Exists(fullAssetPath)) {
                Debug.LogWarning($"Could not find \"{fullAssetPath}\"");
                return;
            }

            var contents = File.ReadAllText(fullAssetPath);
            var hasChanged = false;
            for (var i = 0; i < entry.AssociatedGuids.Length; i++) {
                var guid = entry.AssociatedGuids[i];
                if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing {fullAssetPath}", $"Scrubbing {guid}", (float)i / entry.AssociatedGuids.Length)) {
                    throw new OperationCanceledException();
                }
                
                if (!guids.TryGetValue(guid, out var newGuid)) {
                    continue;
                }

                contents = contents.Replace(guid, newGuid.to.Guid);
                hasChanged = true;
                // Debug.Log($"   - {guid} -> {newGuid.to.Guid}");
            }

            if (!hasChanged) return;
            
            File.WriteAllText(fullAssetPath, contents);
            
            Debug.Log($" - wrote to {fullAssetPath}");
        }

        // public static string GetProjectGameAssetsPath(UPPatcherSettings settings) {
        //     var projectGameAssetsPath = settings.ProjectGameAssetsPath;
        //     projectGameAssetsPath = projectGameAssetsPath.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1);
        //     projectGameAssetsPath = projectGameAssetsPath.Replace('/', Path.DirectorySeparatorChar);
        //     return projectGameAssetsPath;
        // }
        
        public static string? GetProjectPathFromExportPath(AssetCatalogue.Entry entry, UPPatcherSettings settings, AssetRipperSettings arSettings, bool ignoreExclude) {
            return GetProjectPathFromExportPath(settings.ProjectGameAssetsPath, entry, settings, arSettings, ignoreExclude);
        }
        
        public static string? GetProjectPathFromExportPath(string projectGameAssetsPath, AssetCatalogue.Entry entry, UPPatcherSettings settings, AssetRipperSettings arSettings, bool ignoreExclude) {
            var splitPath = entry.RelativePathToRoot.Split(Path.DirectorySeparatorChar);
            var sourceName = splitPath[0];
            if (!arSettings.TryGetFolderMappingFromSource(sourceName, out var folder, out var exclude, Path.Combine("Unknown", sourceName))) {
                Debug.LogWarning($"Could not find folder mapping for {sourceName}");
                return null;
            }
            
            Debug.Log($" - {sourceName} -> {folder}");

            if (!ignoreExclude && exclude) {
                return null;
            }
            
            var restOfPath = string.Join(Path.DirectorySeparatorChar.ToString(), splitPath.Skip(1));
            return Path.Combine(projectGameAssetsPath, folder, restOfPath);
        }
        
        // public readonly static Regex NamespacePattern = new(@"namespace\s+(?<namespace>[\w\.]+)", RegexOptions.Compiled);
        // public readonly static Regex ClassPattern = new(@"class\s+(?<class>[\w\.]+)", RegexOptions.Compiled);
        // public readonly static Regex StructPattern = new(@"struct\s+(?<struct>[\w\.]+)", RegexOptions.Compiled);
        // public readonly static Regex InterfacePattern = new(@"interface\s+(?<interface>[\w\.]+)", RegexOptions.Compiled);
        //
        // private static string? GetFullTypeNameFromDisk(string assetPath) {
        //     // scrub meta file directly (?)
        //     var fullAssetPath = Path.GetFullPath(assetPath);
        //     var fullScriptPath = $"{fullAssetPath}.cs";
        //     if (!File.Exists(fullScriptPath)) return null;
        //
        //     var contents = File.ReadAllText(fullScriptPath);
        //     var namespaceMatch = NamespacePattern.Match(contents);
        // }
    }
}