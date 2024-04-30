using Cysharp.Threading.Tasks;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public readonly struct RestartEditorStep: IPatcherStep {
        public UniTask<StepResult> Run() {
            return UniTask.FromResult(StepResult.RestartEditor);
        }
    }
}