using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using Nomnom.UnityProjectPatcher.Editor.Steps.PreProcessing;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// The core of the patcher. Executes various steps in sequence.
    /// </summary>
    public static class PatcherSteps {
        private readonly static IPatcherStep[] _defaultSteps = {
            new ExtractProjectInformationStep(),
            new ExtractAssetsStep(),
            new CopyGameDLLsStep(),
        };
        
        public static async UniTask Run() {
            // todo: determine which step to start on
            var allSteps = _defaultSteps;
            foreach (var step in allSteps) {
                StepResult result;
                try {
                    result = await step.TryPatch();
                } catch (System.Exception) {
                    result = StepResult.Failure;
                }

                if (result.HasFlag(StepResult.Failure)) {
                    break;
                }
                
                if (result.HasFlag(StepResult.RestartEditor)) {
                    // todo: restart editor
                    break;
                }
            }
        }
    }
}