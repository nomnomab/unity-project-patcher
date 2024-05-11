using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct EnableUnsafeCodeStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            EditorApplication.LockReloadAssemblies();
            PatcherUtility.LockedAssemblies = true;
            PlayerSettings.allowUnsafeCode = true;
            return UniTask.FromResult(StepResult.Recompile);
        }

        public void OnComplete(bool failed) { }
    }
}