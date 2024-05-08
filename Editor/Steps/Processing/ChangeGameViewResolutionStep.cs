using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor.Steps {
    /// <summary>
    /// Allows you to change the game view resolution to an aspect ratio, or a specific resolution,
    /// but either have to already exist in the GameView resolution dropdown.
    /// <br/><br/>
    /// Examples being: 16:9, 16:10, etc
    /// </summary>
    public readonly struct ChangeGameViewResolutionStep: IPatcherStep {
        private readonly string _size;
        
        public ChangeGameViewResolutionStep(string size) {
            _size = size;
        }
        
        public UniTask<StepResult> Run() {
            if (_size is null) {
                Debug.LogWarning("GameView resolution not set");
                return UniTask.FromResult(StepResult.Success);
            }
            
            if (GameViewUtils.TrySetSize(_size)) {
                Debug.Log($"GameView resolution set to {_size}");
            } else {
                Debug.LogWarning($"Could not set GameView resolution to {_size}");
            }
            
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}