using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lachee.Utilities.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

//! instead, make a map of paths to instance ids and somehow remap them afterwards

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Attempts to migrate all prior file ids to the new project assets if applicable.
    /// <br/><br/>
    /// Requires a <see cref="CacheProjectCatalogueStep" /> step near the start of your game wrapper!
    /// </summary>
    public readonly struct FixProjectFileIdsStep: IPatcherStep {
        // [MenuItem("Tools/UPP/Fix File IDs (Will be removed)")]
        // public static void FixFileIds() {
        //     try {
        //         var step = new FixProjectFileIdsStep();
        //         step.Run().Forget();
        //     } catch (Exception e) {
        //         Debug.LogException(e);
        //         EditorUtility.ClearProgressBar();
        //     }
        // }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var startingScene = EditorSceneManager.GetSceneAt(0);
            var startingScenePath = startingScene.path;
            var allScenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            // var allPrefabs = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets" });
            var projectGamePath = settings.ProjectGameAssetsPath;
            var existingCatalogue = GuidRemapperStep.ProjectCatalogue ?? AssetCatalogue.FromDisk(CacheProjectCatalogueStep.ExportPath);
            // var currentCatalogue = GuidRemapperStep.ProjectCatalogue ?? AssetScrubber.ScrubProject();

            var sceneIndex = -1;
            foreach (var sceneGUID in allScenes) {
                sceneIndex++;
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);

                EditorUtility.DisplayProgressBar($"Fixing {scenePath}", $"Scanning {scenePath}", (float)sceneIndex / allScenes.Length);
                
                if (scenePath.StartsWith(projectGamePath)) {
                    continue;
                }

                var scenePathWithoutAssets = PatcherUtility.GetPathWithoutRoot(scenePath);
                var sceneFromCatalogue = existingCatalogue.Entries.FirstOrDefault(x => x.RelativePathToRoot == scenePathWithoutAssets);
                if (sceneFromCatalogue == null) {
                    Debug.LogError($"Could not find scene in catalogue: {scenePath}");
                    continue;
                }

                // var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var oldFileIds = sceneFromCatalogue.FileIds;
                var newFileIds = AssetScrubber.GetFileIdsFromProjectAsset(scenePath).ToArray();
                
                if (oldFileIds.Length != newFileIds.Length) {
                    Debug.LogError($"File id length does not match for {scenePath}. Old: {oldFileIds.Length}, New: {newFileIds.Length}");
                    continue;
                }

                // if (!EditorUtility.DisplayDialog(scenePathWithoutAssets, $"Old: {oldFileIds.Length}\nNew: {newFileIds.Length}", "OK", "Quit")) {
                //     throw new OperationCanceledException();
                // }

                var sceneContents = File.ReadAllText(Path.GetFullPath(scenePath));
                for (int i = 0; i < oldFileIds.Length; i++) {
                    var oldFileId = oldFileIds[i];
                    var newFileId = newFileIds[i].ToString();
                    
                    EditorUtility.DisplayProgressBar($"Fixing {scenePath}", $"Migrating {oldFileId} -> {newFileId}", (float)i / oldFileIds.Length);
                    
                    if (oldFileId == newFileId) continue;
                    
                    Debug.Log($"Migrating {oldFileId} -> {newFileId}");
                    sceneContents = sceneContents
                        .Replace($"fileID: {oldFileId},", $"fileID: {newFileId},")
                        .Replace($"fileID: {oldFileId}}}", $"fileID: {newFileId}}}");
                }
                
                File.WriteAllText(Path.GetFullPath(scenePath), sceneContents);
            }
            
            EditorUtility.ClearProgressBar();
            
            // foreach (var prefabGUID in allPrefabs) {
            //     var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            //     if (prefabPath.StartsWith(projectGamePath)) {
            //         continue;
            //     }
            //     
            //     var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            //     if (!prefab) continue;
            // }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}