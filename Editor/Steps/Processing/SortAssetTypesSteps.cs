using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.AssetRipper;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_3_OR_NEWER
using UnityEngine.Pool;
#endif
using Object = UnityEngine.Object;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct SortAssetTypesSteps: IPatcherStep {
        // [MenuItem("Tools/Unity Project Patcher/Sort Asset Types")]
        // private static void Foo() {
        //     try {
        //         new SortAssetTypesSteps().Run().Forget();
        //     } catch {
        //         EditorUtility.ClearProgressBar();
        //         throw;
        //     }
        // }
        //
        // [MenuItem("Tools/Unity Project Patcher/Unsort Folders")]
        // private static void UnsortFolders() {
        //     var settings = PatcherUtility.GetSettings();
        //     var arSettings = PatcherUtility.GetAssetRipperSettings();
        //     UnsortFolder(settings.ProjectGameAssetsPath, "MonoBehaviour", "ScriptableObject", arSettings);
        //     UnsortFolder(settings.ProjectGameAssetsPath, "PrefabInstance", "Prefab", arSettings);
        // }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();

            SortScriptableObjects(settings, arSettings);
            SortPrefabs(settings, arSettings);
            
            return UniTask.FromResult(StepResult.Success);
        }

        private void SortScriptableObjects(UPPatcherSettings settings, AssetRipperSettings arSettings) {
            // collect all assets
            if (!arSettings.TryGetFolderMapping("MonoBehaviour", out var monoBehaviourFolder, out var exclude) || exclude) {
                Debug.LogWarning("Could not find \"MonoBehaviour\" folder mapping");
                return;
            }
            
            var assets = AssetDatabase.FindAssets("t:ScriptableObject", new string[] {
                Path.Combine(settings.ProjectGameAssetsPath, monoBehaviourFolder).ToAssetDatabaseSafePath()
            })
                .Select(x => (guid: x, assetPath: AssetDatabase.GUIDToAssetPath(x)))
                .ToArray();

            var map = GatherTypeMap(assets, x => new [] { x }, x => x == typeof(ScriptableObject));
            var finalMap = GatherFinalTypeMap(map);
            
            CopyAsstsToNewPaths(finalMap, monoBehaviourFolder, settings);
            Debug.Log("Sorting complete");
        }

        private void SortPrefabs(UPPatcherSettings settings, AssetRipperSettings arSettings) {
            // collect all assets
            if (!arSettings.TryGetFolderMapping("PrefabInstance", out var prefabFolder, out var exclude) || exclude) {
                Debug.LogWarning("Could not find \"PrefabInstance\" folder mapping");
                return;
            }
            
            var assets = AssetDatabase.FindAssets("t:Prefab", new string[] {
                Path.Combine(settings.ProjectGameAssetsPath, prefabFolder).ToAssetDatabaseSafePath()
            })
                .Select(x => (guid: x, assetPath: AssetDatabase.GUIDToAssetPath(x)))
                .ToArray();

            var map = GatherTypeMap(assets, x => (x as GameObject).GetComponents<MonoBehaviour>(), x => x == typeof(GameObject) || (!string.IsNullOrEmpty(x.Namespace) && x.Namespace == "UnityEngine"));
            var finalMap = GatherFinalTypeMap(map);
            
            CopyAsstsToNewPaths(finalMap, prefabFolder, settings);
            Debug.Log("Sorting complete");
        }

        private Dictionary<Type, List<string>> GatherTypeMap((string guid, string assetPath)[] assets, Func<Object, IEnumerable<Object>> getObjects, Func<Type, bool> isBadType) {
            var map = new Dictionary<Type, List<string>>();
            for (var i = 0; i < assets.Length; i++) {
                var asset = assets[i];
                
                EditorUtility.DisplayProgressBar($"Gathering types [{i}/{assets.Length}]", $"Gathering {asset.assetPath}", i / (float)assets.Length);
                
                var (guid, assetPath) = asset;
                Object obj = null;

                try {
                    if (!AssetDatabase.IsMainAssetAtPathLoaded(assetPath)) {
                        obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    } else {
                        obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    }

                    if (!obj) continue;

                    foreach (var type in getObjects(obj).Where(x => x != null).SelectMany(x => x.GetType().GetParentTypes().Where(y => !y.IsInterface))) {
                        if (isBadType(type) || type == typeof(UnityEngine.Object) || type == typeof(System.Object)) continue;
                        if (!map.TryGetValue(type, out var list)) {
                            list = new List<string>();
                            map.Add(type, list);
                        }

                        list.Add(assetPath);
                    }
                } catch {
                    throw;
                }
                finally {
                    if (obj && !(obj is GameObject || obj is Component || obj is AssetBundle)) {
                        Resources.UnloadAsset(obj);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();

            return map;
        }
        
        private Dictionary<string, Type> GatherFinalTypeMap(Dictionary<Type, List<string>> map) {
            var finalMap = new Dictionary<string, Type>();
            foreach (var group in map.OrderByDescending(x => x.Key.Assembly.GetName().Name == "Assembly-CSharp").ThenByDescending(x => x.Value.Count)) {
                EditorUtility.DisplayProgressBar($"Gathering final types", $"Gathering for {group.Key.Name}", 0.5f);
                
                // Debug.Log($"{group.Key.Name} -> {group.Value.Count}");
                foreach (var value in group.Value) {
                    if (!finalMap.ContainsKey(value)) {
                        finalMap.Add(value, group.Key);
                    }
                }
            }
            
            EditorUtility.ClearProgressBar();

            return finalMap;
        }

        private void CopyAsstsToNewPaths(Dictionary<string, Type> finalMap, string keyFolder, UPPatcherSettings settings) {
            foreach (var group in finalMap.GroupBy(x => x.Value).Where(x => x.Count() >= 4)) {
                var folder = group.Key;
                var folderPath = Path.Combine(settings.ProjectGameAssetsPath, keyFolder, folder.Name).ToAssetDatabaseSafePath();
                
                if (!AssetDatabase.IsValidFolder(folderPath)) {
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(folderPath).ToAssetDatabaseSafePath(), Path.GetFileNameWithoutExtension(folderPath));
                }

                AssetDatabase.StartAssetEditing();
                var count = group.Count();
                var index = -1;
                foreach (var kv in group) {
                    var (assetPath, _) = (kv.Key, kv.Value);
                    index++;
                    var assetName = Path.GetFileName(assetPath);
                    
                    EditorUtility.DisplayProgressBar($"Copying assets [{index}/{count}]", $"Moving {assetPath}", index / (float)count);
                    
                    var newAssetPath = Path.Combine(folderPath, assetName).ToAssetDatabaseSafePath();
                    // Debug.Log($"{assetPath} -> {newAssetPath}");
                    
                    AssetDatabase.MoveAsset(assetPath, newAssetPath);
                }
                
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();

                // Debug.Log($"[{group.Key}]:");
                // foreach (var value in group) {
                //     Debug.Log($" - {value}");
                // }
            }
        }

        public static void UnsortFolder(string rootPath, string key, string type, AssetRipperSettings arSettings) {
            if (!arSettings.TryGetFolderMapping(key, out var folder, out var exclude)) {
                return;
            }
            
            var folderPath = Path.Combine(rootPath, folder).ToAssetDatabaseSafePath();
            if (!AssetDatabase.IsValidFolder(folderPath)) {
                return;
            }
            
            AssetDatabase.StartAssetEditing();
#if UNITY_2020_3_OR_NEWER
            using var _ = HashSetPool<string>.Get(out var folders);      
#else
            var folders = new HashSet<string>();
#endif
            var subFolders = AssetDatabase.GetSubFolders(folderPath);
            for (var i = 0; i < subFolders.Length; i++) {
                var subFolder = subFolders[i];
                EditorUtility.DisplayProgressBar($"Unsorting [{i}/{subFolders.Length}]", $"Unsorting {subFolder}", i / (float)subFolders.Length);
                var innerAssets = AssetDatabase.FindAssets($"t:{type}", new string[] {
                    subFolder
                }).Select(x => AssetDatabase.GUIDToAssetPath(x));

                folders.Add(subFolder);

                foreach (var assetPath in innerAssets) {
                    var assetName = Path.GetFileName(assetPath);
                    EditorUtility.DisplayProgressBar($"Unsorting [{i}/{subFolders.Length}]", $"Moving {assetName}", i / (float)subFolders.Length);
                    var newAssetPath = Path.Combine(rootPath, folder, assetName).ToAssetDatabaseSafePath();
                    AssetDatabase.MoveAsset(assetPath, newAssetPath);
                }
            }
            AssetDatabase.StopAssetEditing();
            
            EditorUtility.ClearProgressBar();

            // destroy folders
            EditorUtility.DisplayProgressBar("Unsorting", $"Deleting {folders.Count} subfolders", 1);
#if UNITY_2020_3_OR_NEWER
            AssetDatabase.DeleteAssets(folders.Where(x => AssetDatabase.FindAssets(string.Empty, new string[] { x }).Length == 0).ToArray(), new List<string>());
#else
            foreach (var f in folders.Where(x => AssetDatabase.FindAssets(string.Empty, new string[] { x }).Length == 0).ToArray()) {
                AssetDatabase.DeleteAsset(f);
            }
#endif
            EditorUtility.ClearProgressBar();
        }

        public void OnComplete(bool failed) { }
    }
}