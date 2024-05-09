// using Cysharp.Threading.Tasks;
//
// namespace Nomnom.UnityProjectPatcher.Editor.Steps {
//     /// <summary>
//     /// This simply restarts the editor.
//     /// </summary>
//     public readonly struct RestartEditorStep: IPatcherStep {
//         public UniTask<StepResult> Run() {
//             return UniTask.FromResult(StepResult.RestartEditor);
//         }
//         
//         public void OnComplete(bool failed) { }
//     }
// }