using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lachee.Utilities.Serialization;
using Nomnom.UnityProjectPatcher.AssetRipper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

#if UNITY_2020_3_OR_NEWER
using UnityEngine.Pool;
#endif

namespace Nomnom.UnityProjectPatcher.Editor {
    public static class AssetScrubber {
        private readonly static string[] _emptySearchFolder = Array.Empty<string>();
        private readonly static string[] _assetsRootSearchFolder = new[] {
            "Assets"
        };
        
        // private readonly static HashSet<string> IgnoreScriptFolders = new HashSet<string>() {
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

        private readonly static HashSet<string> IgnoreContains = new HashSet<string>() {
            // "Editor",
            "Test",
        };
        
        private readonly static HashSet<string> IgnoreDiskContains = new HashSet<string>() {
            // "Editor",
            // "Test",
            "Profile-",
            "Profiling.",
            "Unity.Services."
            // "Unity.",
        };
        
        private readonly static HashSet<string> IgnoreEndsWith = new HashSet<string>() {
            // "Attribute.cs",
        };

        // make a hashset
        private readonly static HashSet<string> IgnoreFileExtensionsForProjectAssets = new HashSet<string>() {
            ".txt",
            ".md",
            ".json",
            ".unitypackage",
            ".scenetemplate"
        };

        private readonly static HashSet<string> IgnoreFileExtensionsForAssetAssociations = new HashSet<string>() {
            ".unitypackage",
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
            ".m4v",
            ".mov",
            ".webm",
            ".mp3",
            ".ogg",
            ".wav",
            ".fbx",
            ".obj",
            ".glb",
            ".gltf",
            ".bundle",
            ".dll",
            ".so",
            ".exe",
            ".ttf",
            ".otf",
            ".svg",
            ".txt",
            ".json",
            ".md",
            ".cgnic",
            ".uss",
            ".hlsl",
            ".uxml",
            ".raytrace",
            ".rendertexture",
            ".scenetemplate"
        };

        private readonly static HashSet<Type> QuickSkipTypes = new HashSet<Type>() {
            typeof(Texture),
            typeof(Texture2D),
            typeof(Texture3D),
            typeof(AudioClip),
            typeof(VideoClip),
            typeof(Font),
            typeof(Mesh),
            typeof(Sprite),
            typeof(ComputeShader),
            typeof(Cubemap),
            typeof(RenderTexture),
            typeof(Texture2DArray),
            typeof(WebCamTexture),
        };

        private readonly static HashSet<string> QuickSkipTypeExtensions = new HashSet<string>() {
            ".cgnic",
            ".uss",
            ".hlsl",
            ".uxml",
            ".raytrace",
            ".rendertexture",
        };

        // private readonly static HashSet<string> IgnoreFileExtensionsForAddressableAssetScrubbing = new HashSet<string>() {
        //     ".ogg",
        //     ".controller",
        //     ".anim",
        //     ".mat",
        //     ".overrideController",
        //     ".asset",
        //     ".unity",
        //     ".webm",
        //     ".bundle",
        //     ".shader",
        //     ".cs",
        //     ".asmdef",
        //     ".png",
        //     ".prefab",
        //     ".ttf",
        //     ".otf",
        //     ".h",
        //     ".cpp",
        //     ".mask",
        //     ".svg",
        //     ".Z",
        //     ".json",
        //     ".dat",
        //     ".so",
        //     ".xml",
        //     ".pbs",
        //     ".txt",
        //     ".renderTexture",
        //     ".mixer",
        //     ".dll",
        //     ".exe",
        //     ".md",
        //     ".cff",
        //     ".physicMaterial",
        //
        // };
        
        private readonly static Regex GuidPattern = new Regex(@"guid:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled);
        private readonly static Regex FileIdPattern = new Regex(@"fileID:\s(?<fileId>[0-9A-Za-z]+)", RegexOptions.Compiled);
        private readonly static Regex FileIdReferencePattern = new Regex(@"--- !u!\w+\s&(?<fileId>[0-9A-Za-z]+)", RegexOptions.Compiled);
        private readonly static Regex AddressableGuidPattern = new Regex(@"m_AssetGUID:\s(?<guid>[0-9A-Za-z]+)", RegexOptions.Compiled);
        private readonly static Regex AssetBundleNamePattern = new Regex(@"assetBundleName:\s(?<assetBundleName>[0-9A-Za-z]+)", RegexOptions.Compiled);

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
            try {
                var catalogue = ScrubProject();
                Debug.Log(catalogue);

                var outputPath = Path.Combine(Application.dataPath, "scrub.project.txt");
                File.WriteAllText(outputPath, catalogue.ToString(false));
                AssetDatabase.Refresh();
            } catch {
                EditorUtility.ClearProgressBar();
                throw;
            }
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
            var stopWatch = Stopwatch.StartNew();
            var catalogue = ScrubDiskFolder(disk, arSettings.FoldersToExcludeFromRead);
            Debug.Log($"{stopWatch.ElapsedMilliseconds}ms ({stopWatch.Elapsed.TotalSeconds}sec)");
            Debug.Log(catalogue);
            
            var outputPath = Path.Combine(Application.dataPath, "scrub.disk_custom.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Scrub/Addressables")]
        public static void TestScrubDiskFolderAddressables() {
            var arSettings = PatcherUtility.GetAssetRipperSettings();
            var disk = arSettings.OutputExportAssetsFolderPath;
            var stopWatch = Stopwatch.StartNew();
            try {
                ScrubDiskAddressables(disk, arSettings.FoldersToExcludeFromRead);
            } catch (Exception e) {
                Debug.LogError(e);
            }
            Debug.Log($"{stopWatch.ElapsedMilliseconds}ms ({stopWatch.Elapsed.TotalSeconds}sec)");
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
            var stopWatch = Stopwatch.StartNew();
            for (var i = 0; i < searchInFolders.Length; i++) {
                searchInFolders[i] = searchInFolders[i].ToAssetDatabaseSafePath();
            }
            
#if UNITY_2020_3_OR_NEWER
            using var _ = ListPool<AssetCatalogue.Entry>.Get(out var entries);
#else
            var entries = new List<AssetCatalogue.Entry>();
#endif
            
            entries.Clear();
            
            EditorUtility.DisplayProgressBar("Scrubbing Project", "Scrubbing assets", 0);

            var assets = AssetDatabase.FindAssets(searchQuery, searchInFolders)
                .Select(x => (guid: x, path: AssetDatabase.GUIDToAssetPath(x)))
                .Where(x => Path.HasExtension(x.path))
                .Where(x => !IgnoreFileExtensionsForProjectAssets.Any(y => y == Path.GetExtension(x.path)))
                .Where(x => !IgnoreContains.Any(y => x.path.Contains(y)) && !IgnoreEndsWith.Any(y => Path.GetFileNameWithoutExtension(x.path).EndsWith(y)))
                .ToArray();
            
#if UNITY_2020_3_OR_NEWER
            using var __ = ListPool<(MonoScript, string assetPath, long fileId)>.Get(out var nonMonos);
            using var ___ = HashSetPool<string>.Get(out var usedTypes);
#else
            var nonMonos = new List<(MonoScript, string assetPath, long fileId)>();
            var usedTypes = new HashSet<string>();
#endif

            PatcherUtility.StartProfiler();
            Profiler.BeginSample("Scrubbing Assets");
            
            EditorUtility.DisplayProgressBar("Scrubbing Project", $"Scrubbing {assets.Length} assets...", 0);
            // var weightedBad = new WeightedBag<string, (string path, long loadMs)>();
            // var loadStopWatch = Stopwatch.StartNew();
            for (var i = 0; i < assets.Length; i++) {
                var (assetGuid, path) = assets[i];
                if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing Project [{i}/{assets.Length}]", $"Scrubbing {path}", i / (float)assets.Length)) {
                    throw new OperationCanceledException();
                }
                
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType is null) continue;

                var properPath = path.ToOSPath();

#if UNITY_2020_3_OR_NEWER
                UnityEngine.Object? obj = null;
#else
                UnityEngine.Object obj = null;
#endif
                try {
                    var fullPath = Path.GetFullPath(path);
                    var assetPathNoAssets = PatcherUtility.GetPathWithoutRoot(path);
                    
                    // materials are dumb and tend to crash when imported???
                    if (path.EndsWith(".mat")) {
                        entries.Add(new AssetCatalogue.Entry(properPath, assetGuid, 4800000, GetAssociatedGuids(assetPathNoAssets, fullPath, null).ToArray(), GetFileIdsFromProjectAsset(path).Select(x => x.ToString()).ToArray()));
                        continue;
                    }

                    // shaders are dumb too???
                    if (path.EndsWith(".shader")) {
                        var shaderType = GetShaderName(fullPath, null);
                        entries.Add(new AssetCatalogue.ShaderEntry(properPath, assetGuid, 4800000, shaderType, null, null));
                        continue;
                    }
                    
                    if (QuickSkipTypes.Contains(assetType) || QuickSkipTypeExtensions.Contains(Path.GetExtension(path))) {
                        var fileGuid = GetGuidFromDisk(fullPath);
                        entries.Add(new AssetCatalogue.Entry(properPath, fileGuid, null, null, null));
                        continue;
                    }
                    
                    // loadStopWatch.Restart();
                    if (!AssetDatabase.IsMainAssetAtPathLoaded(path)) {
                        obj = AssetDatabase.LoadMainAssetAtPath(path);
                    } else {
                        obj = AssetDatabase.LoadAssetAtPath(path, assetType);
                    }
                    // weightedBad.Add(Path.GetExtension(path), (path, loadStopWatch.ElapsedMilliseconds));
                    
                    var found = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long fileId);
                    if (!found) continue;
                    
                    var associatedGuids = GetAssociatedGuids(assetPathNoAssets, fullPath, null).ToArray();
                    var associatedFileIds = GetFileIdsFromProjectAsset(path).Select(x => x.ToString()).ToArray();
                    
                    if (assetType != typeof(MonoScript)) {
                        if (assetType == typeof(Shader)) {
                            var shaderType = GetShaderName(fullPath, null);
                            entries.Add(new AssetCatalogue.ShaderEntry(properPath, guid, fileId, shaderType, associatedGuids, associatedFileIds));
                        } else {
                            entries.Add(new AssetCatalogue.Entry(properPath, guid, fileId, associatedGuids, associatedFileIds));
                        }
                        continue;
                    }
                    
                    // is a monoscript
                    var monoScript = obj as MonoScript;
                    if (monoScript is null) continue;
                        
                    var foundClass = monoScript.GetClass();
                    nonMonos.Add((monoScript, path.ToOSPath(), fileId));
                        
                    var assemblyName = foundClass?.Assembly.GetName().Name;
                    var nestedTypes = foundClass?.GetNestedTypes()
                        .Select(x => new AssetCatalogue.ScriptEntry(x.FullName ?? "n/a", "n/a", null, x.FullName ?? "n/a", assemblyName ?? "Assembly-CSharp", Array.Empty<AssetCatalogue.ScriptEntry>(), associatedGuids, associatedFileIds))
                        .ToArray() ?? Array.Empty<AssetCatalogue.ScriptEntry>();
                    
                    var typeName = foundClass?.FullName ?? assetType.FullName ?? "n/a";
                    entries.Add(new AssetCatalogue.ScriptEntry(properPath, guid, fileId, foundClass?.FullName ?? assetType.FullName ?? "n/a", assemblyName ?? "Assembly-CSharp", nestedTypes, associatedGuids, associatedFileIds));
                        
                    usedTypes.Add(typeName);
                        
                    foreach (var nestedType in nestedTypes) {
                        if (nestedType is null || nestedType.FullTypeName is null) {
                            continue;
                        }
                            
                        usedTypes.Add(nestedType.FullTypeName);
                    }
                } catch (Exception e) {
                    Debug.LogError($"Failed to load {path}.\n{e}");
                }
                finally {
                    if (obj && !(obj is GameObject || obj is Component || obj is AssetBundle)) {
                        Resources.UnloadAsset(obj);
                    }
                }
            }

            // var orderedEntries = weightedBad.Results.OrderByDescending(x => x.Value.counter).ThenByDescending(x => x.Value.value.Sum(y => y.loadMs));
            // foreach (var entry in orderedEntries) {
            //     var totalMs = entry.Value.value.Sum(x => x.loadMs);
            //     Debug.Log($"{entry.Key} has {entry.Value.value.Count} results ({totalMs}ms):");
            //     foreach (var e in entry.Value.value.OrderByDescending(y => y.loadMs).Take(10)) {
            //         Debug.Log($" - [{e.loadMs}ms] \"{e.path}\"");
            //     }
            // }
            
            EditorUtility.DisplayProgressBar("Scrubbing Project", "Getting all nonMono texts", 0);

            // things that aren't MonoBehaviour but still are found
            var threadSafeNonMonos = nonMonos.Where(x => x.Item1)
                .Select(x => (text: x.Item1.text, x.fileId, x.assetPath))
                .ToArray();

            foreach (var mono in nonMonos) {
                if (mono.Item1) {
                    Resources.UnloadAsset(mono.Item1);
                }
            }
            
            nonMonos.Clear();
            
            var assemblies = GetProjectAssemblies().ToArray();

            EditorUtility.DisplayProgressBar("Scrubbing Project", "Scrubbing nonMonos", 0);
            // var each = Parallel.ForEach(threadSafeNonMonos, x => {
            //     var (text, fileId, assetPath) = x;
            //
            //     foreach (var entry in ScrubNonMonoData(text, assetPath, fileId, assemblies)) {
            //         if (usedTypes.Contains(entry.FullTypeName ?? string.Empty)) {
            //             continue;
            //         }
            //         
            //         nonMonoEntries.Add(entry);
            //     }
            // });
            
            // while (!each.IsCompleted) { }

            for (var i = 0; i < threadSafeNonMonos.Length; i++) {
                var x = threadSafeNonMonos[i];
                var (text, fileId, assetPath) = x;
                if (EditorUtility.DisplayCancelableProgressBar($"Checking for nonMonos [{i}/{threadSafeNonMonos.Length}]", $"Scrubbing {assetPath}", i / (float)threadSafeNonMonos.Length)) {
                    throw new OperationCanceledException();
                }

                foreach (var entry in ScrubNonMonoData(text, assetPath, fileId, assemblies)) {
                    if (EditorUtility.DisplayCancelableProgressBar($"Checking for nonMonos [{i}/{threadSafeNonMonos.Length}]", $"Checking {entry.FullTypeName}", i / (float)threadSafeNonMonos.Length)) {
                        throw new OperationCanceledException();
                    }
                    
                    if (usedTypes.Contains(entry.FullTypeName ?? string.Empty)) {
                        continue;
                    }

                    // nonMonoEntries.Add(entry);
                    entries.Add(entry);
                }
            }

            EditorUtility.ClearProgressBar();

            // foreach (var entry in nonMonoEntries) {
            //     entries.Add(entry);
            // }

            GC.Collect();
            EditorUtility.ClearProgressBar();
            
            Debug.Log($"Scrubbing project took {stopWatch.ElapsedMilliseconds}ms ({stopWatch.ElapsedMilliseconds/1000}sec)");

            return new AssetCatalogue(Application.dataPath, entries);
        }
        
        private static IEnumerable<Assembly> GetProjectAssemblies() {
            var scriptAssembliesPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies");
            var dlls = Directory.GetFiles(scriptAssembliesPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => !IgnoreDiskContains.Any(x.Contains));
            // .ToArray();
            
            // foreach (var dll in dlls) {
            //     Debug.Log($"Scrubbing {dll}");
            // }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && dlls.Contains(x.GetName().Name));

            return assemblies;
        }

        private static Type[] GetAllTypes() {
            var allTypes = GetProjectAssemblies()
                .SelectMany(x => x.GetValidTypes())
                .ToArray();

            return allTypes;
        }
        
#if UNITY_2020_3_OR_NEWER
        public static string? GetShaderName(string assetPath, string? contents) {
#else
        public static string GetShaderName(string assetPath, string contents) {
#endif
            if (!File.Exists(assetPath)) {
                return null;
            }

            if (string.IsNullOrEmpty(contents)) {
                contents = File.ReadAllText(assetPath);
            }
            
            var regex = new Regex(@"Shader\s+.(?<TypeName>.*\w)");
            var match = regex.Match(contents);
            if (!match.Success) {
                return null;
            }
            
            return match.Groups["TypeName"].Value.Trim();
        }
        
        public static IEnumerable<AssetCatalogue.ScriptEntry> ScrubNonMonoData(MonoScript nonMono, string assetPath, long fileId) {
            return ScrubNonMonoData(nonMono.text, assetPath, fileId, GetProjectAssemblies().ToArray());
        }

        public static IEnumerable<AssetCatalogue.ScriptEntry> ScrubNonMonoData(string text, string assetPath, long fileId, Assembly[] assemblies) {
            var foundNamespace = GetNamespace(text);
            var foundAll = GetDefinitions(text, "class", "struct", "interface", "enum");
            var foundDelegates = GetDelegateDefinitions(text);

            var combinedTypes = foundAll.Select(x => $"{foundNamespace}.{x}")
                .Concat(foundDelegates.Select(x => $"{foundNamespace}.{x}"))
                .ToArray();

            foreach (var type in combinedTypes) {
                // check if this type exists
                foreach (var asm in assemblies) {
                    var t = asm.GetType(type, false, true);
                    if (t is null) continue;
                    
                    yield return new AssetCatalogue.ScriptEntry(assetPath, string.Empty, fileId, t.FullName, t.Assembly.GetName().Name, Array.Empty<AssetCatalogue.ScriptEntry>(), null, null);
                    break;
                }
            }
        }

#if UNITY_2020_3_OR_NEWER
        private static string? GetNamespace(string text) {
#else
        private static string GetNamespace(string text) {
#endif
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
        
        private static IEnumerable<string> GetDefinitions(string text, params string[] names) {
#if UNITY_2020_3_OR_NEWER
            var namesString = $"({string.Join('|', names)})";
#else
            var namesString = $"({string.Join("|", names)})";
#endif
            var regexGeneric = new Regex(namesString + @"\s+(?<TypeName>\w+)(?:\s*<(?<GenericParameters>\w+(?:,\s*\w+)*)?>)");
            
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
            
            var regex = new Regex(namesString + @"\s+(?<TypeName>\w+)");

            // no generics
            foreach (var match in regex.Matches(text).Cast<Match>()) {
                if (!match.Success) continue;
                yield return match.Groups["TypeName"].Value.Trim();
            }
        }

        public static AssetCatalogue ScrubDiskFolder(string folderPath, IEnumerable<string> foldersToExclude) {
            var stopWatch = Stopwatch.StartNew();
            EditorUtility.DisplayProgressBar("Scrubbing Folder", $"Scrubbing {folderPath}", 0);
            
            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) != ".meta" && !IgnoreEndsWith.Any(x.EndsWith))
                .Where(File.Exists)
                .Select(x => (file: x, relativeFile: x.Substring(folderPath.Length + 1)))
                .Where(x => {
                    var relativeFile = x.relativeFile;
                    if (foldersToExclude.Any(y => relativeFile.StartsWith(y))) {
                        return false;
                    }

                    if (IgnoreDiskContains.Any(y => relativeFile.Contains(y))) {
                        return false;
                    }

                    return true;
                })
                .ToArray();
            
            EditorUtility.DisplayProgressBar($"Scrubbing {files.Length} files from disk", $"Grabbing entries... this will take a while!", 0);

            var entries = new ConcurrentBag<AssetCatalogue.Entry>();
            var each = Parallel.ForEach(files, x => {
                var (file, relativeFile) = x;
                var extension = Path.GetExtension(relativeFile);
                // var contents = File.ReadAllText(file);
                var guid = GetGuidFromDisk(file);
                
                switch (extension) {
                    case ".cs": {
                        //! I hate this :)
                        // if (rootFolder != "Scripts") continue;
                        var typeName = PatcherUtility.GetPathWithoutRoot(relativeFile);
                    
                        // take first folder
                        var assemblyName = typeName.Substring(0, typeName.IndexOf('\\'));
                        // if (foldersToExclude.Contains(assemblyName)) continue;
                    
                        // trim first folder
                        var fullTypeName = typeName.Substring(assemblyName.Length + 1);
                        // trim extension
                        fullTypeName = fullTypeName.Substring(0, fullTypeName.LastIndexOf('.'));
                        // make a type path
                        fullTypeName = fullTypeName.Replace('\\', '.');
                        
                        var contents = File.ReadAllText(file);
                        var foundNamespace = GetNamespace(contents);
                        foreach (var e in GetDefinitions(contents, "class").Concat(GetDefinitions(contents, "struct")).Take(1)) {
                            fullTypeName = $"{foundNamespace}.{e}";
                        }
                        
                        var associatedGuids = GetAssociatedGuids(relativeFile, file, null).ToArray();
                        var associatedFileIds = GetFileIdsFromDisk(file).ToArray();
                        entries.Add(new AssetCatalogue.ScriptEntry(relativeFile, guid, null, fullTypeName, assemblyName, Array.Empty<AssetCatalogue.ScriptEntry>(), associatedGuids, associatedFileIds));
                    }
                        break;
                    case ".shader": {
                        var shaderType = GetShaderName(file, null);
                        // var associatedGuids = GetAssociatedGuids(relativeFile, file, null).ToArray();
                        // var associatedFileIds = GetFileIdsFromDisk(file).ToArray();
                        entries.Add(new AssetCatalogue.ShaderEntry(relativeFile, guid, null, shaderType, null, null));
                    }
                        break;
                    default: {
                        if (IgnoreFileExtensionsForAssetAssociations.Contains(Path.GetExtension(file))) {
                            entries.Add(new AssetCatalogue.Entry(relativeFile, guid, null, null, null));
                        } else {
                            var associatedGuids = GetAssociatedGuids(relativeFile, file, null).ToArray();
                            var associatedFileIds = GetFileIdsFromDisk(file).ToArray();
                            entries.Add(new AssetCatalogue.Entry(relativeFile, guid, null, associatedGuids, associatedFileIds));
                        }
                    }
                        break;
                }
            });
            
            while (!each.IsCompleted) { }

            // var index = -1;
            // foreach (var entry in entries) {
            //     index++;
            //     if (EditorUtility.DisplayCancelableProgressBar("Scrubbing Project", $"Getting associations for {entry.RelativePathToRoot}", index / (float)entries.Count)) {
            //         throw new OperationCanceledException();
            //     }
            //     
            //     // shaders -> don't need them
            //     if (Path.GetExtension(entry.RelativePathToRoot) == ".shader") {
            //         continue;
            //     }
            //
            //     var path = entry.RelativePathToRoot;
            //     var fullPath = Path.Combine(folderPath, path);
            //     Debug.Log(fullPath);
            //     var associatedGuids = GetAssociatedGuids(path, fullPath, null).ToArray();
            //     var associatedFileIds = GetFileIdsFromDisk(fullPath).ToArray();
            //
            //     entry.AssociatedGuids = associatedGuids;
            //     entry.FileIds = associatedFileIds;
            // }

            EditorUtility.ClearProgressBar();
            Debug.Log($"Scrubbing disk took {stopWatch.Elapsed.Seconds} seconds");
            stopWatch.Stop();

            return new AssetCatalogue(folderPath, entries);
        }

        public static void ScrubDiskAddressables(string folderPath, IEnumerable<string> foldersToExclude) {
            var stopWatch = Stopwatch.StartNew();
            var settings = PatcherUtility.GetSettings();
            var keysPath = Path.Combine(settings.GameDataPath, "Addressables_Rip", "0_ResourceLocationMap_AddressablesMainContentCatalog_keys.txt");
            var bundlesPath = Path.Combine(settings.GameDataPath, "Addressables_Rip", "0_ResourceLocationMap_AddressablesMainContentCatalog_locations.txt");
            
            EditorUtility.DisplayProgressBar("Scrubbing for Addressables", $"Collecting files...", 0);
            
            var metaFiles = Directory.GetFiles(folderPath, "*.meta", SearchOption.AllDirectories)
                .Where(x => !x.EndsWith(".cs.meta") && !IgnoreEndsWith.Any(x.EndsWith))
                .Where(File.Exists)
                .Select(x => (file: x, relativeFile: x.Substring(folderPath.Length + 1)))
                .Where(x => {
                    var relativeFile = x.relativeFile;
                    if (foldersToExclude.Any(y => relativeFile.StartsWith(y))) {
                        return false;
                    }

                    if (IgnoreDiskContains.Any(y => relativeFile.Contains(y))) {
                        return false;
                    }

                    return true;
                })
                .ToArray();
            
            var idLookup = new ConcurrentDictionary<string, HashSet<string>>();
            var each = Parallel.ForEach(metaFiles, x => {
                var (file, relativeFile) = x;
                
                var contents = File.ReadAllText(file);
                var match = AssetBundleNamePattern.Match(contents);
                if (!match.Success) return;
                
                var assetBundleName = match.Groups["assetBundleName"].Value;
                if (string.IsNullOrEmpty(assetBundleName)) return;
                
                if (!idLookup.TryGetValue(assetBundleName, out var list)) {
                    idLookup[assetBundleName] = list = new HashSet<string>();
                }
                
                list.Add(file);
            });
            while (!each.IsCompleted) { }
            
            // foreach (var kvp in idLookup) {
            //     Debug.Log($"{kvp.Key} -> {kvp.Value.Count}");
            // }
            
            using (var reader = new StreamReader(keysPath)) {
                string lastLine = string.Empty;
                string line;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    
                    EditorUtility.DisplayProgressBar("Scrubbing for Addressables", $"Scrubbing {line}", 0);

                    if (line.Length == 32) {
                        if (idLookup.TryGetValue(line, out var files)) {
                            var index = -1;
                            foreach (var file in files) {
                                index++;
                                
                                EditorUtility.DisplayProgressBar("Scrubbing for Addressables", $"Fixing {file}", index / (float) files.Count);
                                
                                var content = File.ReadAllText(file);
                                content = AssetBundleNamePattern.Replace(content, $"assetBundleName: {lastLine}");
                                File.WriteAllText(file, content);
                                
                                // Debug.Log($"Fixing {file}, \"{line}.bundle\" -> \"{lastLine}.bundle\"");
                            }
                        }
                    }
                    
                    lastLine = line;
                }
            }
            
            using (var reader = new StreamReader(bundlesPath)) {
                Queue<string> last = new Queue<string>();
                string line;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    
                    EditorUtility.DisplayProgressBar("Scrubbing for Addressables", $"Scrubbing {line}", 0);

                    if (line.Length == 32) {
                        if (idLookup.TryGetValue(line, out var files)) {
                            var bundleName = Path.GetFileName(last.First());
                            if (bundleName.EndsWith(".bundle")) {
                                bundleName = Path.GetFileNameWithoutExtension(bundleName);
                            }
                            
                            var index = -1;
                            foreach (var file in files) {
                                index++;
                                
                                EditorUtility.DisplayProgressBar("Scrubbing for Addressables", $"Fixing {file}", index / (float) files.Count);
                                
                                var content = File.ReadAllText(file);
                                content = AssetBundleNamePattern.Replace(content, $"assetBundleName: {bundleName}");
                                File.WriteAllText(file, content);
                                
                                // Debug.Log($"Fixing {file}, \"{line}.bundle\" -> \"{bundleName}\"");
                            }
                        }
                    }
                    
                    last.Enqueue(line);
                    if (last.Count > 2) {
                        last.Dequeue();
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();
            
            Debug.Log($"Scrubbing addressables took {stopWatch.Elapsed.Seconds} seconds");
        }
        
        private static bool IsGenericScript(string assetPath, string typeName) {
            if (!File.Exists(assetPath)) return false;
            var assetContents = File.ReadAllText(assetPath);
            return assetContents.Contains($" {typeName}<");
        }
        
#if UNITY_2020_3_OR_NEWER
        public static string? GetGuidFromDisk(string assetPath) {
#else
        public static string GetGuidFromDisk(string assetPath) {
#endif
            // scrub meta file directly (?)
            // var fullAssetPath = Path.GetFullPath(assetPath);
            var fullMetaPath = $"{assetPath}.meta";
            if (!File.Exists(fullMetaPath)) return null;

            var contents = File.ReadAllText(fullMetaPath);
            var match = GuidPattern.Match(contents);
            if (!match.Success) return null;
                
            // grab guid from disk
            var actualGuid = match.Groups["guid"].Value;
            if (string.IsNullOrEmpty(actualGuid)) return null;

            return actualGuid;
        }
        
        // public static IEnumerable<string> GetFileIdReferences(string assetPath) {
        //     yield break;
        // }
        
        public static IEnumerable<string> GetFileIdsFromDisk(string fullAssetPath) {
            if (!File.Exists(fullAssetPath)) {
                yield break;
            }

            if (!HasValidAssociationExtension(fullAssetPath)) {
                yield break;
            }
            
            var contents = File.ReadAllText(fullAssetPath);
            var matches = FileIdReferencePattern.Matches(contents);
            
            foreach (var match in matches.Cast<Match>()) {
                if (!match.Success) continue;
                
                var fileId = match.Groups["fileId"].Value;
                if (!string.IsNullOrEmpty(fileId)) {
                    yield return fileId;
                }
            }
        }

        public static IEnumerable<string> GetFileIdsFromProjectAsset(string assetPath) {
            if (!HasValidAssociationExtension(assetPath)) {
                yield break;
            }
            
            if (assetPath.StartsWith("Packages")) {
                yield break;
            }

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            var originalScenePath = EditorSceneManager.GetActiveScene().path;

            if (assetType == typeof(SceneAsset)) {
                Scene scene;
                if (assetPath == originalScenePath) {
                    scene = EditorSceneManager.GetSceneByPath(assetPath);
                } else {
                    scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                }
                
                foreach (var _ in scene.GetRootGameObjects()) {
                    var objId = GlobalObjectId.GetGlobalObjectIdSlow(_);
                    var id = objId.targetObjectId;
                    
                    if (id < 9999) continue;
                    yield return id.ToString();
                    
                    foreach (var c in _.GetComponentsInChildren<Component>()) {
                        var globalId = GlobalObjectId.GetGlobalObjectIdSlow(c);
                        if (globalId.assetGUID != null) {
                            if (globalId.targetObjectId < 9999) continue;
                            yield return globalId.targetObjectId.ToString();
                        }
                    }
                }

                if (assetPath != originalScenePath) {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            yield break;
        }
        
#if UNITY_2020_3_OR_NEWER
        private static IEnumerable<string> GetAssociatedGuids(string assetPath, string fullAssetPath, string? contents) {
#else
        private static IEnumerable<string> GetAssociatedGuids(string assetPath, string fullAssetPath, string contents) {
#endif
            // var fullMetaPath = $"{fullAssetPath}.meta";
            if (!File.Exists(fullAssetPath)) {
                yield break;
            }

            if (!HasValidAssociationExtension(assetPath)) {
                yield break;
            }
            
            // var assetContents = File.ReadAllText(fullAssetPath);
            if (string.IsNullOrEmpty(contents)) {
                contents = File.ReadAllText(fullAssetPath);
            }
            
            var matches = GuidPattern.Matches(contents);
            
            foreach (var match in matches.Cast<Match>()) {
                if (!match.Success) continue;
                
                var guid = match.Groups["guid"].Value;
                if (!string.IsNullOrEmpty(guid)) {
                    yield return guid;
                }
            }
            
            // matches = AddressableGuidPattern.Matches(contents);
            //
            // foreach (var match in matches.Cast<Match>()) {
            //     if (!match.Success) continue;
            //     
            //     var guid = match.Groups["guid"].Value;
            //     if (!string.IsNullOrEmpty(guid)) {
            //         yield return guid;
            //     }
            // }
        }

        private static bool HasValidAssociationExtension(string assetPath) {
            var extension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (IgnoreFileExtensionsForAssetAssociations.Any(x => extension == x)) {
                return false;
            }

            if (assetPath.Contains("\\")) {
                var rootFolder = assetPath.Substring(0, assetPath.IndexOf('\\'));
                if (rootFolder == "Mesh") {
                    return false;
                }
            }

            return true;
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

        public static void ReplaceAssetGuids(UPPatcherSettings settings, string rootPath, AssetCatalogue.Entry entry, Dictionary<string, AssetCatalogue.Entry> entryMap) {
            if (!HasValidAssociationExtension(entry.RelativePathToRoot)) {
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
                
                if (!entryMap.TryGetValue(guid, out var newEntry)) {
                    continue;
                }
                
                
                // replace guid in entire file
                contents = contents.Replace(guid, newEntry.Guid);
                
                hasChanged = true;
                // Debug.Log($"   - {guid} -> {newGuid.to.Guid}");
            }

            if (!hasChanged) return;
            
            File.WriteAllText(fullAssetPath, contents);
            
            Debug.Log($" - wrote to {fullAssetPath}");
        }

        // public static IEnumerable<(string from, string to)> ReplaceNewSceneFileIds(string scenePath) {
        //     yield break;
        // }
        //     // var currentScene = EditorSceneManager.GetActiveScene();
        //     // var sceneFullPath = Path.GetFullPath(currentScene.path);
        //     var sceneContents = File.ReadAllText(scenePath);
        //     var yamlComponents = UYAMLParser.Parse(sceneContents);
        //     var foundProperties = new Dictionary<string, UObject>();
        //     foreach (var component in yamlComponents) {
        //         if (component.classID != UClassID.PrefabInstance) continue;
        //         foundProperties.Clear();
        //         // Debug.Log(component);
        //
        //         if (!component.Component.properties.TryGetValue("m_Modification", out var modification)) {
        //             continue;
        //         }
        //         
        //         var modificationObj = modification.value as UObject;
        //         if (!modificationObj.properties.TryGetValue("m_Modifications", out var modifications)) {
        //             continue;
        //         }
        //         
        //         var modificationsArray = modifications.value as UArray;
        //         // var remove = new List<UObject>();
        //         foreach (var m in modificationsArray.items) {
        //             if (m is not UObject obj) continue;
        //             if (!obj.properties.TryGetValue("target", out var target)) {
        //                 continue;
        //             }
        //             
        //             var targetObj = target.value as UObject;
        //             var propertyPathValue = obj.properties["propertyPath"].value as UValue;
        //             // Debug.Log(string.Join(" | ", targetObj.properties));
        //             // Debug.Log(propertyPathValue.value);
        //                             
        //             if (foundProperties.TryGetValue(propertyPathValue.value, out var oldObj)) {
        //                 var oldTarget = oldObj.properties["target"].value as UObject;
        //                 var oldFileID = oldTarget.properties["fileID"].value as UValue;
        //                 var newFileID = targetObj.properties["fileID"].value as UValue;
        //                 
        //                 // Debug.Log($" [duplicate!] - {oldFileID.value} -> {newFileID.value} ({propertyPathValue.value})");
        //                 
        //                 yield return (oldFileID.value, newFileID.value);
        //                 
        //                 // oldFileID.value = newFileID.value;
        //                                 
        //                 // remove.Add(obj);
        //                 foundProperties.Remove(propertyPathValue.value);
        //             } else {
        //                 foundProperties.Add(propertyPathValue.value, obj);
        //             }
        //         }
        //                     
        //         // modificationsArray.items.RemoveAll(x => remove.Contains(x));
        //                     
        //         // var builder = new UYAMLWriter();
        //         // builder.AddComponets(yamlComponents);
        //         //             
        //         // var newContent = builder.ToString();
        //         // // Debug.Log(newContent);
        //         //             
        //         // File.WriteAllText(scenePath, newContent);
        //         // foreach (var o in yamlComponents) {
        //         //     // yield return o;
        //         // }
        //     }
        //
        //     yield break;
        // }

        // public static void ReplaceFileIds(string rootPath, AssetCatalogue.Entry entry, AssetCatalogue.FoundMatch[] matches) {
        //     if (entry.FileIds == null) return;
        //     if (entry.FileIds.Length == 0) return;
        //     
        //     var fullAssetPath = Path.Combine(rootPath, entry.RelativePathToRoot);
        //     var contents = File.ReadAllText(fullAssetPath);
        //     for (var m = 0; m < matches.Length; m++) {
        //         var match = matches[m];
        //         if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing {fullAssetPath}", $"Scrubbing {entry.Guid}", (float)m / matches.Length)) {
        //             throw new OperationCanceledException();
        //         }
        //         
        //         if (match.from.Guid != entry.Guid) {
        //             continue;
        //         }
        //         
        //         if (match.to.FileIds == null) continue;
        //
        //         for (int i = 0; i < match.from.FileIds.Length; i++)
        //         for (int j = 0; j < match.to.FileIds.Length; j++) {
        //             var from = match.from.FileIds[i];
        //             var to = match.to.FileIds[j];
        //             
        //             if (EditorUtility.DisplayCancelableProgressBar($"Scrubbing {fullAssetPath}", $"Migrating {from} to {to}", (float)m / matches.Length)) {
        //                 throw new OperationCanceledException();
        //             }
        //             
        //             contents = contents.Replace($"fileID: {from}", $"fileID: {to}");
        //         }
        //         break;
        //     }
        //
        //     File.WriteAllText(fullAssetPath, contents);
        // }
        
        // public static string GetProjectGameAssetsPath(UPPatcherSettings settings) {
        //     var projectGameAssetsPath = settings.ProjectGameAssetsPath;
        //     projectGameAssetsPath = projectGameAssetsPath.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1);
        //     projectGameAssetsPath = projectGameAssetsPath.Replace('/', Path.DirectorySeparatorChar);
        //     return projectGameAssetsPath;
        // }
        
#if UNITY_2020_3_OR_NEWER
        public static string? GetProjectPathFromExportPath(AssetCatalogue.Entry entry, UPPatcherSettings settings, AssetRipperSettings arSettings, bool ignoreExclude) {
#else
        public static string GetProjectPathFromExportPath(AssetCatalogue.Entry entry, UPPatcherSettings settings, AssetRipperSettings arSettings, bool ignoreExclude) {
#endif
            return GetProjectPathFromExportPath(settings.ProjectGameAssetsPath, entry, settings, arSettings, ignoreExclude);
        }
        
#if UNITY_2020_3_OR_NEWER
        public static string? GetProjectPathFromExportPath(string projectGameAssetsPath, AssetCatalogue.Entry entry, UPPatcherSettings settings, AssetRipperSettings arSettings, bool ignoreExclude) {
#else
        public static string GetProjectPathFromExportPath(string projectGameAssetsPath, AssetCatalogue.Entry entry, UPPatcherSettings settings, AssetRipperSettings arSettings, bool ignoreExclude) {
#endif
            var splitPath = entry.RelativePathToRoot.Split(Path.DirectorySeparatorChar);
            var sourceName = splitPath[0];
            if (!arSettings.TryGetFolderMappingFromSource(sourceName, out var folder, out var exclude, Path.Combine("Unknown", sourceName))) {
                Debug.LogWarning($"Could not find folder mapping for \"{sourceName}\"");
                return null;
            }
            
            // Debug.Log($" - {sourceName} -> {folder}");

            if (!ignoreExclude && exclude) {
                Debug.LogWarning($"Skipping {sourceName}");
                return null;
            }
            
            var restOfPath = string.Join(Path.DirectorySeparatorChar.ToString(), splitPath.Skip(1));
            return Path.Combine(projectGameAssetsPath, folder, restOfPath);
        }

#if UNITY_2020_3_OR_NEWER
        public static string? GetExportPathFromProjectPath(string projectAssetPath, UPPatcherSettings settings, AssetRipperSettings arSettings) {
#else
        public static string GetExportPathFromProjectPath(string projectAssetPath, UPPatcherSettings settings, AssetRipperSettings arSettings) {
#endif
            var pathWithoutRoot = projectAssetPath.Substring(settings.ProjectGameAssetsPath.Length + 1);
            var withoutFile = Path.GetDirectoryName(pathWithoutRoot);
            if (!arSettings.TryGetFolderMappingFromOutput(withoutFile, out var folder, out var exclude)) {
                // check if any of the mappings start with this
                // FolderMapping? found = arSettings.FolderMappings.FirstOrDefault(x => withoutFile.StartsWith(x.outputPath));
                // if (found is null) {
                Debug.LogWarning($"Could not find folder mapping for \"{withoutFile}\"");
                return null;
                // }

                // folder = found.Value.sourceName.ToOSPath();
                // exclude = found.Value.exclude;
            }

            var exportPath = Path.Combine(arSettings.OutputExportAssetsFolderPath, folder, Path.GetFileName(projectAssetPath));
            return exportPath;
        }

        // public static string GetExportPathFromAddressablePath(AssetRipperSettings arSettings, string addressablePath) {
        //     // first is assets
        //     // second is folder
        //     // rest is inner path
        //     // var startingFolder = PatcherUtility.GetStartingFolders(addressablePath, 2);
        //     // var restOfPath = addressablePath.Substring(startingFolder.Length);
        //     // var withoutAssets = Path.GetFileName(startingFolder);
        //
        // }
        
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