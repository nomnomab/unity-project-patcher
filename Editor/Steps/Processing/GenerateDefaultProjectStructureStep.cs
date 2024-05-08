using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Sets up the default folders that the project will use.
    /// </summary>
    public readonly struct GenerateDefaultProjectStructureStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            var settings = this.GetSettings();
            
            // create paths
            try {
                if (!PatcherUtility.TryToCreatePath(settings.ProjectUnityPath)) {
                    return UniTask.FromResult(StepResult.Failure);
                }
            
                if (!PatcherUtility.TryToCreatePath(settings.ProjectUnityAssetStorePath)) {
                    return UniTask.FromResult(StepResult.Failure);
                }
                
                if (!PatcherUtility.TryToCreatePath(settings.ProjectGameFullPath)) {
                    return UniTask.FromResult(StepResult.Failure);
                }
                
                if (!PatcherUtility.TryToCreatePath(settings.ProjectGameAssetsFullPath)) {
                    return UniTask.FromResult(StepResult.Failure);
                }
                
                if (!PatcherUtility.TryToCreatePath(settings.ProjectGameModsFullPath)) {
                    return UniTask.FromResult(StepResult.Failure);
                }
                
                if (!PatcherUtility.TryToCreatePath(settings.ProjectGameToolsFullPath)) {
                    return UniTask.FromResult(StepResult.Failure);
                }
            } catch {
                Debug.LogError("Failed to create default project paths");
                throw;
            }
            
            return UniTask.FromResult(StepResult.Success);
        }
        
        public void OnComplete(bool failed) { }
    }
}