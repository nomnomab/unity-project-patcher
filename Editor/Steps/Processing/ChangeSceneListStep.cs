using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Gathers all of the scenes in the project and injects them into the scene list,
    /// and will put a scene first in the list by name, if provided.
    /// </summary>
    public readonly struct ChangeSceneListStep: IPatcherStep {
        private readonly string _firstSceneName;
        
        public ChangeSceneListStep(string firstSceneName) {
            _firstSceneName = firstSceneName;
        }
        
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            var arSettings = this.GetAssetRipperSettings();
            
            if (!arSettings.TryGetFolderMapping("Scenes", out var sceneFolder, out var exclude) || exclude) {
                Debug.LogError("Could not find \"Scenes\" folder mapping");
                return UniTask.FromResult(StepResult.Success);
            }

            var scenes = AssetDatabase.FindAssets("t:Scene", new[] {
                Path.Combine(settings.ProjectGameAssetsPath, sceneFolder).ToAssetDatabaseSafePath()
            })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            var firstSceneName = _firstSceneName;
            if (!string.IsNullOrEmpty(_firstSceneName)) {
                var firstScene = scenes.FirstOrDefault(x => x.Contains(firstSceneName));
                if (firstScene is null) {
                    Debug.LogError($"Could not find scene with name \"{_firstSceneName}\"");
                    return UniTask.FromResult(StepResult.Success);
                }

                scenes = scenes.Where(scene => scene != firstScene)
                    .Prepend(firstScene)
                    .ToArray();
            }

            EditorBuildSettings.scenes = scenes
                .Select(scene => new EditorBuildSettingsScene(scene, true))
                .ToArray();
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}