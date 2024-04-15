using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Sets up the default folders that the project will use.
    /// </summary>
    public readonly struct GenerateDefaultProjectStructureStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            if (settings.GameName is null) {
                Debug.LogError($"Game name was null. Assign it in your {nameof(UPPatcherSettings)} asset! Click here to focus it.", settings);
                return UniTask.FromResult(StepResult.Failure);
            }

            var assetsPath = Application.dataPath;
            // where default unity assets go, as well as asset store ones
            var baseUnityPath = Path.GetFullPath(Path.Combine(assetsPath, "Unity"));
            // where assets can go from the store
            var baseUnityAssetStorePath = Path.Combine(assetsPath, "AssetStore");
            // where assets that started with the project can go
            var baseUnityNativePath = Path.Combine(assetsPath, "Native");
            
            var baseGamePath = Path.GetFullPath(Path.Combine(assetsPath, settings.GameName.Replace(" ", string.Empty)));
            // where assets go from the base game
            var baseGameAssetsPath = Path.Combine(baseGamePath, "Game");
            // where user mods go
            var baseGameModsPath = Path.Combine(baseGamePath, "Mods");
            // where things like plugins go
            var baseGameToolsPath = Path.Combine(baseGamePath, "Tools");
            
            // create paths
            if (!PatcherUtility.TryToCreatePath(baseUnityPath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            if (!PatcherUtility.TryToCreatePath(baseUnityAssetStorePath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            if (!PatcherUtility.TryToCreatePath(baseUnityNativePath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            if (!PatcherUtility.TryToCreatePath(baseGamePath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            if (!PatcherUtility.TryToCreatePath(baseGameAssetsPath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            if (!PatcherUtility.TryToCreatePath(baseGameModsPath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            if (!PatcherUtility.TryToCreatePath(baseGameToolsPath)) {
                return UniTask.FromResult(StepResult.Failure);
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
    }
}