using Cysharp.Threading.Tasks;

namespace Nomnom.UnityProjectPatcher.Editor.Steps.PreProcessing {
    [BeforeStep("")]
    public class ExtractProjectInformationStep: IPatcherStep {
        public UniTask<StepResult> TryPatch() {
            throw new System.NotImplementedException();
        }
    }
}