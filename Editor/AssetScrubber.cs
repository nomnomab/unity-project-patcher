using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Nomnom.UnityProjectPatcher.Editor {
    public static class AssetScrubber {
        private readonly static string[] _emptySearchFolder = Array.Empty<string>();
        private readonly static string[] _assetsRootSearchFolder = new[] {
            "Assets"
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
            var catalogue = ScrubDiskFolder(Application.dataPath);
            Debug.Log(catalogue);
            
            var outputPath = Path.Combine(Application.dataPath, "scrub.disk.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Scrub/Disk - Custom")]
        public static void TestScrubDiskFolderCustom() {
            var disk = EditorUtility.OpenFolderPanel("Scrub Folder", "Assets", "");
            if (string.IsNullOrEmpty(disk)) return;
            var catalogue = ScrubDiskFolder(disk);
            Debug.Log(catalogue);
            
            var outputPath = Path.Combine(Application.dataPath, "scrub.disk_custom.txt");
            File.WriteAllText(outputPath, catalogue.ToString(false));
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Scrub/Compare")]
        public static void TestScrubCompareTwoProjects() {
            var disk = EditorUtility.OpenFolderPanel("Scrub Folder", "Assets", "");
            if (string.IsNullOrEmpty(disk)) return;
            
            var diskCatalogue = ScrubDiskFolder(disk);
            var projectCatalogue = ScrubProject();
            
            Debug.Log(diskCatalogue);
            Debug.Log(projectCatalogue);
            diskCatalogue.Compare(projectCatalogue);
        }
        
        public static AssetCatalogue ScrubProjectAssets() {
            return ScrubProject(string.Empty, _assetsRootSearchFolder);
        }

        public static AssetCatalogue ScrubProject() {
            return ScrubProject(string.Empty, _emptySearchFolder);
        }

        public static AssetCatalogue ScrubProject(string searchQuery, string[] searchInFolders) {
            using var _ = ListPool<AssetCatalogue.Entry>.Get(out var entries);
            
            EditorUtility.DisplayProgressBar("Scrubbing Project", "Scrubbing Assets", 0);

            var assetGuids = AssetDatabase.FindAssets(searchQuery, searchInFolders);
            for (var i = 0; i < assetGuids.Length; i++) {
                var assetGuid = assetGuids[i];
                EditorUtility.DisplayProgressBar("Scrubbing Project", $"Scrubbing {assetGuid}", i / (float) assetGuids.Length);
                
                // load type information
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (!Path.HasExtension(assetPath)) continue;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType is null) continue;

                UnityEngine.Object? obj = null;
                try {
                    obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    var found = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long fileId);
                    if (!found) continue;

                    if (assetType != typeof(MonoScript)) {
                        entries.Add(new AssetCatalogue.Entry(assetPath, guid, fileId));
                    } else {
                        var monoScript = obj as MonoScript;
                        if (monoScript is null) continue;

                        var foundClass = monoScript.GetClass();
                        entries.Add(new AssetCatalogue.ScriptEntry(assetPath, guid, fileId, foundClass?.FullName ?? assetType.FullName ?? "n/a"));
                    }
                } catch (Exception e) {
                    Debug.LogError($"Failed to load {assetPath}.\n{e}");
                    throw;
                }
                finally {
                    if (obj) {
                        Resources.UnloadAsset(obj);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();

            return new AssetCatalogue(Application.dataPath, entries);
        }
        
        public static AssetCatalogue ScrubDiskFolder(string folderPath) {
            using var _ = ListPool<AssetCatalogue.Entry>.Get(out var entries);
            
            EditorUtility.DisplayProgressBar("Scrubbing Folder", $"Scrubbing {folderPath}", 0);
            
            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++) {
                var file = files[i];
                EditorUtility.DisplayProgressBar("Scrubbing Folder", $"Scrubbing {file}", i / (float) files.Length);
                
                var guid = GetGuidFromDisk(file);
                if (guid is null) continue;

                entries.Add(new AssetCatalogue.Entry(file[(folderPath.Length + 1)..], guid, null));
            }
            
            EditorUtility.ClearProgressBar();

            return new AssetCatalogue(folderPath, entries);
        }

        private static string? GetGuidFromDisk(string assetPath) {
            // scrub meta file directly (?)
            var fullAssetPath = Path.GetFullPath(assetPath);
            var fullMetaPath = $"{fullAssetPath}.meta";
            if (!File.Exists(fullMetaPath)) return null;
                
            var metaContents = File.ReadAllText(fullMetaPath);
            var match = GuidPattern.Match(metaContents);
            if (!match.Success) return null;
                
            // grab guid from disk
            var actualGuid = match.Groups["guid"].Value;
            if (string.IsNullOrEmpty(actualGuid)) return null;

            return actualGuid;
        }
    }
}