using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// This simply lets you execute a menu option from the editor, such as "File/Save All".
    /// </summary>
    public readonly struct ExecuteMenuOptionStep: IPatcherStep {
        private readonly string _menuOption;
        
        public ExecuteMenuOptionStep(string menuOption) {
            _menuOption = menuOption;
        }
        
        public UniTask<StepResult> Run() {
            EditorApplication.ExecuteMenuItem(_menuOption);
            return UniTask.FromResult(StepResult.Success);
        }
        
        public void OnComplete(bool failed) { }
    }
}