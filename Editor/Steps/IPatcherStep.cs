using Cysharp.Threading.Tasks;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    public interface IPatcherStep {
        UniTask<StepResult> TryPatch();
    }
}